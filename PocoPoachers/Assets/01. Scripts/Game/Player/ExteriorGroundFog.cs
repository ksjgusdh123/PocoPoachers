using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[ExecuteAlways]
public sealed class ExteriorGroundFog : MonoBehaviour
{
    [SerializeField] private Terrain _terrain;
    [SerializeField] private Shader _shader;
    [SerializeField, Min(0.01f)] private float _maxGroundHeight = 0.6f;
    [SerializeField, Min(0.01f)] private float _fogHeight = 0.8f;
    [SerializeField, Range(0f, 1f)] private float _density = 0.85f;
    [SerializeField] private Color _fogColor = new Color(0.49f, 0.43f, 0.33f, 1f);
    [SerializeField, Min(1f)] private float _noiseScale = 35f;
    private GameObject _surface;
    private Material _material;
    private Texture2D _mask;
    private Mesh _mesh;
    private bool _rebuild;
    public string Status { get; private set; } = "안개 생성 대기 중";

#if UNITY_EDITOR
    private void QueueRebuild()
    {
        UnityEditor.EditorApplication.delayCall -= RebuildInEditor;
        UnityEditor.EditorApplication.delayCall += RebuildInEditor;
    }
    private void RebuildInEditor()
    {
        if (this == null || !isActiveAndEnabled) return;
        _rebuild = false;
        Rebuild();
        UnityEditor.SceneView.RepaintAll();
    }
#endif

    private void OnEnable()
    {
        _rebuild = true;
#if UNITY_EDITOR
        QueueRebuild();
#endif
    }
    private void OnValidate() => _rebuild = true;
    private void Update()
    {
        if (_rebuild) { _rebuild = false; Rebuild(); }
    }

    [ContextMenu("외곽 안개 다시 생성")]
    public void Rebuild()
    {
        Clear();
        if (_terrain == null || _terrain.terrainData == null || _shader == null)
        {
            Status = "Terrain 또는 Shader 연결이 없습니다.";
            return;
        }
        const int resolution = 256;
        var data = _terrain.terrainData;
        var low = new bool[resolution * resolution];
        float minHeight = float.MaxValue, maxHeight = float.MinValue;
        for (int z = 0; z < resolution; z++)
        for (int x = 0; x < resolution; x++)
        {
            float height = data.GetInterpolatedHeight(
                x / (float)(resolution - 1), z / (float)(resolution - 1));
            minHeight = Mathf.Min(minHeight, height);
            maxHeight = Mathf.Max(maxHeight, height);
            low[z * resolution + x] = height <= _maxGroundHeight;
        }

        // 가장 큰 저지대만 선택해 분리된 연못과 내부 구덩이에 안개가 생기는 것을 줄인다.
        var largest = new List<int>();
        var queue = new Queue<int>();
        for (int seed = 0; seed < low.Length; seed++)
        {
            if (!low[seed]) continue;
            var region = new List<int>();
            low[seed] = false;
            queue.Enqueue(seed);
            while (queue.Count > 0)
            {
                int index = queue.Dequeue();
                region.Add(index);
                int x = index % resolution, z = index / resolution;
                if (x > 0) Visit(index - 1);
                if (x < resolution - 1) Visit(index + 1);
                if (z > 0) Visit(index - resolution);
                if (z < resolution - 1) Visit(index + resolution);
            }
            if (region.Count > largest.Count) largest = region;
        }
        void Visit(int index)
        {
            if (!low[index]) return;
            low[index] = false;
            queue.Enqueue(index);
        }

        if (largest.Count == 0)
        {
            Status = $"표시 영역 없음: 지형 높이 {minHeight:F2}~{maxHeight:F2}, 선택 기준 {_maxGroundHeight:F2}";
            Debug.LogWarning($"[ExteriorGroundFog] {Status}", this);
            return;
        }

        var pixels = new Color32[low.Length];
        foreach (int index in largest) pixels[index] = new Color32(255, 255, 255, 255);
        _mask = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, true)
        { name = "외곽 저지대 마스크", hideFlags = HideFlags.HideAndDontSave,
            wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
        _mask.SetPixels32(pixels);
        _mask.Apply(false, true);
        _material = new Material(_shader) { hideFlags = HideFlags.HideAndDontSave };
        _material.SetTexture("_Mask", _mask);
        _material.SetColor("_FogColor", _fogColor);
        _material.SetFloat("_Density", _density);
        _material.SetFloat("_NoiseScale", _noiseScale);
        _mesh = new Mesh { name = "외곽 안개 평면", hideFlags = HideFlags.HideAndDontSave };
        _mesh.vertices = new[] { Vector3.zero, new Vector3(0, 0, data.size.z),
            new Vector3(data.size.x, 0, data.size.z), new Vector3(data.size.x, 0, 0) };
        _mesh.uv = new[] { Vector2.zero, Vector2.up, Vector2.one, Vector2.right };
        _mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        _mesh.RecalculateBounds();
        // 생성 결과를 Hierarchy에서 확인할 수 있도록 숨기지 않고 저장만 제외한다.
        _surface = new GameObject("외곽 안개 표면 (자동 생성)") { hideFlags = HideFlags.DontSave };
        _surface.layer = gameObject.layer;
        _surface.transform.SetParent(transform, false);
        _surface.transform.position = _terrain.transform.position + Vector3.up * _fogHeight;
        _surface.transform.rotation = Quaternion.identity;
        _surface.AddComponent<MeshFilter>().sharedMesh = _mesh;
        var renderer = _surface.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = _material;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        Status = $"생성됨: 선택 영역 {largest.Count * 100f / low.Length:F1}% / "
            + $"안개 월드 Y {_surface.transform.position.y:F2}\n"
            + $"지형 높이 {minHeight:F2}~{maxHeight:F2} / 색상 알파 {_fogColor.a:F2} / "
            + $"셰이더 지원 {_shader.isSupported}";
        Debug.Log($"[ExteriorGroundFog] {Status}", this);
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall -= RebuildInEditor;
#endif
        Clear();
    }
    private void Clear()
    {
        Release(_surface); Release(_material); Release(_mask); Release(_mesh);
        _surface = null; _material = null; _mask = null; _mesh = null;
    }
    private static void Release(Object value)
    {
        if (value == null) return;
        if (Application.isPlaying) Destroy(value); else DestroyImmediate(value);
    }
}
