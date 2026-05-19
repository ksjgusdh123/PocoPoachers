using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletDecalPool : Singleton<BulletDecalPool>
{
    private const float SurfaceOffset = 0.01f;

    [SerializeField] private GameObject _decalPrefab;
    [SerializeField] private int _defaultCapacity = 32;
    [SerializeField] private int _maxDecals = 80;
    [SerializeField] private float _minSize = 0.12f;
    [SerializeField] private float _maxSize = 0.18f;
    [SerializeField] private float _lifetime = 20f;

    private readonly Queue<BulletDecal> _activeDecals = new();
    private ObjectPool<BulletDecal> _pool;
    private GameObject _runtimeDefaultPrefab;

    public void Spawn(RaycastHit hit)
    {
        if (hit.collider == null) return;

        EnsurePool();
        ReleaseOldestIfNeeded();

        BulletDecal decal = _pool.Get();
        Vector3 position = hit.point + hit.normal * SurfaceOffset;
        Quaternion rotation = Quaternion.LookRotation(hit.normal) * Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
        float size = Random.Range(_minSize, _maxSize);

        decal.Place(position, rotation, hit.collider.transform, size, _lifetime, () => _pool.Release(decal));
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

        if (!decalObject.TryGetComponent(out BulletDecal decal))
            decal = decalObject.AddComponent<BulletDecal>();

        return decal;
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

        return material;
    }
}
