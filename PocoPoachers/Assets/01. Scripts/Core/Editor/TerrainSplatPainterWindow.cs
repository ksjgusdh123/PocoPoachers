using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 씬의 Terrain에 레이어를 물리고 스플랫맵을 규칙 기반으로 칠하는 에디터 툴.
// TerrainData는 바이너리 에셋이라 텍스트로 편집할 수 없어 이 창을 통해서만 갱신한다.
public class TerrainSplatPainterWindow : EditorWindow
{
    [SerializeField] TerrainLayer sandA;
    [SerializeField] TerrainLayer sandB;
    [SerializeField] TerrainLayer gravel;
    [SerializeField] TerrainLayer rock;

    // 모래 A/B 블렌드 — 값이 작을수록 얼룩이 커진다(월드 미터 기준 주파수)
    [SerializeField] float blendNoiseScale = 0.004f;
    [SerializeField] float blendContrast = 1.6f;
    [SerializeField] float sandBAmount = 0.3f;

    [SerializeField] float gravelNoiseScale = 0.011f;
    [SerializeField] float gravelAmount = 0.3f;

    [SerializeField] float rockSlopeStart = 22f;
    [SerializeField] float rockSlopeFull = 42f;

    [SerializeField] int seed = 1234;
    [SerializeField] bool onlySelected;

    Vector2 scroll;

    [MenuItem("Tools/Generator/Terrain Splat Painter")]
    public static void Open() => GetWindow<TerrainSplatPainterWindow>("Terrain Splat");

    void OnEnable()
    {
        sandA ??= Load("Sand_A");
        sandB ??= Load("Sand_B");
        gravel ??= Load("Gravel");
        rock ??= Load("Rock");
    }

    static TerrainLayer Load(string name)
        => AssetDatabase.LoadAssetAtPath<TerrainLayer>(
            $"Assets/_Art/Environment/Textures/Terrain/{name}.terrainlayer");

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("레이어", EditorStyles.boldLabel);
        sandA = Field("모래 A (베이스)", sandA);
        sandB = Field("모래 B (변주)", sandB);
        gravel = Field("자갈 (선택)", gravel);
        rock = Field("암반 (선택)", rock);
        EditorGUILayout.HelpBox("비워둔 슬롯은 건너뛴다. 모래 A는 필수.", MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("모래 A/B 블렌드", EditorStyles.boldLabel);
        blendNoiseScale = EditorGUILayout.Slider("얼룩 주파수", blendNoiseScale, 0.001f, 0.03f);
        SizeHint(blendNoiseScale);
        blendContrast = EditorGUILayout.Slider("경계 대비", blendContrast, 1f, 5f);
        sandBAmount = EditorGUILayout.Slider("모래 B 면적 비율", sandBAmount, 0.05f, 0.95f);

        if (gravel != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("자갈", EditorStyles.boldLabel);
            gravelNoiseScale = EditorGUILayout.Slider("패치 주파수", gravelNoiseScale, 0.002f, 0.05f);
            SizeHint(gravelNoiseScale);
            gravelAmount = EditorGUILayout.Slider("면적 비율", gravelAmount, 0f, 0.8f);
        }

        if (rock != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("암반 (경사도 기준)", EditorStyles.boldLabel);
            rockSlopeStart = EditorGUILayout.Slider("시작 각도", rockSlopeStart, 0f, 89f);
            rockSlopeFull = EditorGUILayout.Slider("완전 전환 각도", rockSlopeFull, 1f, 90f);
            rockSlopeFull = Mathf.Max(rockSlopeFull, rockSlopeStart + 1f);
        }

        EditorGUILayout.Space();
        seed = EditorGUILayout.IntField("시드", seed);
        onlySelected = EditorGUILayout.Toggle("선택한 Terrain만", onlySelected);

        var targets = CollectTargets();
        EditorGUILayout.LabelField($"대상 Terrain: {targets.Count}개");

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(sandA == null || targets.Count == 0))
        {
            if (GUILayout.Button("스플랫 칠하기", GUILayout.Height(32)))
                PaintAll(targets);
        }
        EditorGUILayout.HelpBox("기존 터레인 레이어와 스플랫맵을 전부 덮어쓴다. Ctrl+Z로 되돌릴 수 있다.",
            MessageType.Warning);

        EditorGUILayout.EndScrollView();
    }

    // 슬라이더 값이 작을수록 얼룩이 커지는 역방향이라 실제 크기를 같이 보여준다
    static void SizeHint(float noiseScale)
        => EditorGUILayout.LabelField(" ", $"얼룩 하나 약 {1f / noiseScale:F0} m", EditorStyles.miniLabel);

    static TerrainLayer Field(string label, TerrainLayer value)
        => (TerrainLayer)EditorGUILayout.ObjectField(label, value, typeof(TerrainLayer), false);

    List<Terrain> CollectTargets()
    {
        if (onlySelected)
            return Selection.gameObjects
                .Select(go => go.GetComponent<Terrain>())
                .Where(t => t != null).ToList();

        return FindObjectsByType<Terrain>(FindObjectsSortMode.None)
            .Where(t => t.terrainData != null).ToList();
    }

    void PaintAll(List<Terrain> targets)
    {
        var layers = new List<TerrainLayer> { sandA };
        int idxB = Add(layers, sandB);
        int idxGravel = Add(layers, gravel);
        int idxRock = Add(layers, rock);

        try
        {
            for (int i = 0; i < targets.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Terrain Splat",
                    $"{targets[i].name} 칠하는 중...", (float)i / targets.Count);
                Paint(targets[i], layers.ToArray(), idxB, idxGravel, idxRock);
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[TerrainSplat] Terrain {targets.Count}개에 레이어 {layers.Count}장 적용 완료");
    }

    static int Add(List<TerrainLayer> layers, TerrainLayer layer)
    {
        if (layer == null) return -1;
        layers.Add(layer);
        return layers.Count - 1;
    }

    void Paint(Terrain terrain, TerrainLayer[] layers, int idxB, int idxGravel, int idxRock)
    {
        var data = terrain.terrainData;
        Undo.RegisterCompleteObjectUndo(data, "Paint Terrain Splat");
        data.terrainLayers = layers;

        int res = data.alphamapResolution;
        int count = layers.Length;
        var maps = new float[res, res, count];

        Vector3 origin = terrain.transform.position;
        Vector3 size = data.size;

        // 노이즈를 월드 좌표로 샘플링해야 타일로 나뉜 여러 Terrain을 가로질러 무늬가 이어진다
        float offset = seed * 0.7717f;

        for (int z = 0; z < res; z++)
        {
            float nz = (float)z / (res - 1);
            for (int x = 0; x < res; x++)
            {
                float nx = (float)x / (res - 1);
                float wx = origin.x + nx * size.x;
                float wz = origin.z + nz * size.z;

                float remain = 1f;

                if (idxRock >= 0)
                {
                    float steep = data.GetSteepness(nx, nz);
                    float w = Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(rockSlopeStart, rockSlopeFull, steep));
                    maps[z, x, idxRock] = w;
                    remain -= w;
                }

                if (idxGravel >= 0)
                {
                    float n = Noise(wx, wz, gravelNoiseScale, offset + 137f);
                    float w = remain * Mathf.SmoothStep(0f, 1f,
                        Mathf.InverseLerp(1f - gravelAmount, 1f - gravelAmount * 0.4f, n));
                    maps[z, x, idxGravel] = w;
                    remain -= w;
                }

                float blend = 0f;
                if (idxB >= 0)
                {
                    // 펄린 값이 0.5 근처에 몰려 있어, 임계값을 옮겨 B가 차지할 면적을 조절한다
                    float threshold = Mathf.Lerp(0.78f, 0.22f, sandBAmount);
                    blend = Noise(wx, wz, blendNoiseScale, offset);
                    blend = Mathf.Clamp01((blend - threshold) * blendContrast + 0.5f);
                    maps[z, x, idxB] = remain * blend;
                }
                maps[z, x, 0] = remain * (1f - blend);
            }
        }

        data.SetAlphamaps(0, 0, maps);
        EditorUtility.SetDirty(data);
    }

    // 옥타브 2개를 겹쳐 얼룩 경계가 너무 규칙적으로 보이지 않게 한다
    static float Noise(float x, float z, float scale, float offset)
    {
        float a = Mathf.PerlinNoise(x * scale + offset, z * scale + offset);
        float b = Mathf.PerlinNoise(x * scale * 2.7f + offset * 1.3f, z * scale * 2.7f + offset * 1.3f);
        return Mathf.Clamp01(a * 0.7f + b * 0.3f);
    }
}
