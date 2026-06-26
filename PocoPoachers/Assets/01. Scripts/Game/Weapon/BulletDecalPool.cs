using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.VFX;

public class BulletDecalPool : Singleton<BulletDecalPool>
{
    private const float SurfaceOffset = 0.01f;

    [SerializeField] private GameObject _decalPrefab;
    [SerializeField] private VisualEffectAsset _decalVfxAsset;
    [SerializeField] private int _defaultCapacity = 32;
    [SerializeField] private int _maxDecals = 80;
    [SerializeField] private float _decalSize = 0.36f;
    [SerializeField] private float _particleSize = 3.0f;
    [SerializeField] private float _lifetime = 0.2f;
    [SerializeField] private float _popDuration = 0.07f;
    [SerializeField] private float _popScale = 2.5f;
    [SerializeField] private float _fadeDuration = 0.1f;
    [SerializeField] private float _vfxLifetime = 1f;

    private readonly Queue<BulletDecal> _activeDecals = new();
    private ObjectPool<BulletDecal> _pool;
    private ObjectPool<VisualEffect> _vfxPool;
    private GameObject _runtimeDefaultPrefab;

    public void Spawn(RaycastHit hit, Color color = default)
    {
        if (hit.collider == null) return;

        EnsurePool();
        ReleaseOldestIfNeeded();

        BulletDecal decal = _pool.Get();
        Vector3 position = hit.point + hit.normal * SurfaceOffset;
        Quaternion decalRotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        Quaternion vfxRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

        PlayVfx(position, vfxRotation, hit.collider.transform, color);

        decal.Place(
            position,
            decalRotation,
            hit.collider.transform,
            _decalSize,
            _lifetime,
            _popDuration,
            _popScale,
            _fadeDuration,
            () => _pool.Release(decal),
            color);
        _activeDecals.Enqueue(decal);
    }

    private void EnsurePool()
    {
        if (_pool != null) return;
        if (_decalPrefab == null)
            _decalPrefab = CreateDefaultDecalPrefab();

        _pool = new ObjectPool<BulletDecal>(
            createFunc: CreateDecal,
            actionOnGet: decal => decal.gameObject.SetActive(true),
            actionOnRelease: decal => decal.gameObject.SetActive(false),
            actionOnDestroy: decal => Destroy(decal.gameObject),
            defaultCapacity: _defaultCapacity,
            maxSize: _maxDecals
        );
    }

    private BulletDecal CreateDecal()
    {
        GameObject decalObject = Instantiate(_decalPrefab);
        decalObject.name = _decalPrefab.name;

        ConfigureRenderer(decalObject);

        if (!decalObject.TryGetComponent(out BulletDecal decal))
            decal = decalObject.AddComponent<BulletDecal>();

        return decal;
    }

    private void PlayVfx(Vector3 position, Quaternion rotation, Transform parent, Color color)
    {
        if (_decalVfxAsset == null) return;

        EnsureVfxPool();

        VisualEffect visualEffect = _vfxPool.Get();
        Transform visualEffectTransform = visualEffect.transform;
        visualEffectTransform.SetParent(parent, true);
        visualEffectTransform.SetPositionAndRotation(position, rotation);
        Vector3 ls = parent != null ? parent.lossyScale : Vector3.one;
        visualEffectTransform.localScale = new Vector3(_particleSize / ls.x, _particleSize / ls.y, _particleSize / ls.z);

        visualEffect.Reinit();
        if (color != default && visualEffect.HasVector4("Color"))
            visualEffect.SetVector4("Color", color);
        visualEffect.SendEvent("OnPlay");
        StartCoroutine(ReleaseVfxAfterDelay(visualEffect));
    }

    private void EnsureVfxPool()
    {
        if (_vfxPool != null) return;

        _vfxPool = new ObjectPool<VisualEffect>(
            createFunc: CreateVfx,
            actionOnGet: visualEffect => visualEffect.gameObject.SetActive(true),
            actionOnRelease: visualEffect =>
            {
                visualEffect.Stop();
                visualEffect.transform.SetParent(transform, true);
                visualEffect.gameObject.SetActive(false);
            },
            actionOnDestroy: visualEffect => Destroy(visualEffect.gameObject),
            defaultCapacity: _defaultCapacity,
            maxSize: _maxDecals
        );
    }

    private VisualEffect CreateVfx()
    {
        GameObject visualEffectObject = new GameObject("BulletDecalVFX");
        visualEffectObject.transform.SetParent(transform, false);

        VisualEffect visualEffect = visualEffectObject.AddComponent<VisualEffect>();
        visualEffect.visualEffectAsset = _decalVfxAsset;
        return visualEffect;
    }

    private System.Collections.IEnumerator ReleaseVfxAfterDelay(VisualEffect visualEffect)
    {
        yield return new WaitForSeconds(_vfxLifetime);

        if (visualEffect != null)
            _vfxPool.Release(visualEffect);
    }

    private static void ConfigureRenderer(GameObject decalObject)
    {
        Renderer renderer = decalObject.GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        foreach (Material material in renderer.sharedMaterials)
        {
            if (material == null) continue;
            ConfigureMaterial(material);
        }
    }

    private void ReleaseOldestIfNeeded()
    {
        while (_activeDecals.Count >= _maxDecals)
        {
            BulletDecal oldestDecal = _activeDecals.Dequeue();
            if (oldestDecal != null && oldestDecal.IsSpawned)
            {
                oldestDecal.Release();
                return;
            }
        }
    }

    private GameObject CreateDefaultDecalPrefab()
    {
        if (_runtimeDefaultPrefab != null) return _runtimeDefaultPrefab;

        _runtimeDefaultPrefab = new GameObject("BulletDecal");
        _runtimeDefaultPrefab.SetActive(false);
        _runtimeDefaultPrefab.transform.SetParent(transform, false);

        MeshFilter meshFilter = _runtimeDefaultPrefab.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = CreateDefaultMesh();

        MeshRenderer meshRenderer = _runtimeDefaultPrefab.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = CreateDefaultMaterial();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;

        _runtimeDefaultPrefab.AddComponent<BulletDecal>();
        return _runtimeDefaultPrefab;
    }

    private static Mesh CreateDefaultMesh()
    {
        const int segmentCount = 14;
        Vector3[] vertices = new Vector3[segmentCount + 1];
        int[] triangles = new int[segmentCount * 3];

        vertices[0] = Vector3.zero;
        for (int i = 0; i < segmentCount; i++)
        {
            float angle = i / (float)segmentCount * Mathf.PI * 2f;
            float radius = i % 2 == 0 ? 0.5f : 0.38f;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
        }

        for (int i = 0; i < segmentCount; i++)
        {
            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i + 1;
            triangles[triangleIndex + 2] = i == segmentCount - 1 ? 1 : i + 2;
        }

        Mesh mesh = new Mesh
        {
            name = "BulletDecalMesh",
            vertices = vertices,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private static Material CreateDefaultMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Unlit/Color")
            ?? Shader.Find("Sprites/Default");

        Material material = new Material(shader)
        {
            name = "BulletDecalRuntimeMaterial"
        };

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", new Color(0.03f, 0.025f, 0.02f, 1f));
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", new Color(0.03f, 0.025f, 0.02f, 1f));

        ConfigureMaterial(material);

        return material;
    }

    private static void ConfigureMaterial(Material material)
    {
        if (material.HasProperty("_Cull"))
            material.SetFloat("_Cull", (float)UnityEngine.Rendering.CullMode.Off);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
}
