using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// 평평한 Terrain에 완만한 사구와 잔굴곡을 더하는 에디터 툴.
// 기존 높이에 더하는 방식이라 이미 만든 언덕은 유지된다. 칠하기는 TerrainSplatPainterWindow가 맡는다.
public class TerrainDuneNoiseWindow : EditorWindow
{
    [SerializeField] float duneHeight = 1.5f;
    [SerializeField] float duneScale = 0.012f;
    [SerializeField] float ridgeSharpness = 0.3f;
    [SerializeField] float detailHeight = 0.25f;
    [SerializeField] float detailScale = 0.08f;

    [SerializeField] bool protectObjects = true;
    [SerializeField] float protectMargin = 3f;
    [SerializeField] float protectFalloff = 8f;
    [SerializeField] float minProtectSize = 2f;
    [SerializeField] float maxProtectSize = 80f;

    [SerializeField] bool snapObjects = true;
    [SerializeField] float snapTolerance = 1f;

    [SerializeField] bool backup = true;
    [SerializeField] int seed = 1234;
    [SerializeField] bool onlySelected;

    Vector2 scroll;

    [MenuItem("Tools/Terrain/사구 높낮이 만들기")]
    public static void Open() => GetWindow<TerrainDuneNoiseWindow>("Terrain Dune");

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("사구", EditorStyles.boldLabel);
        duneHeight = EditorGUILayout.Slider("높이 (m)", duneHeight, 0f, 10f);
        duneScale = EditorGUILayout.Slider("주파수", duneScale, 0.002f, 0.05f);
        SizeHint(duneScale);
        ridgeSharpness = EditorGUILayout.Slider("능선 날카로움", ridgeSharpness, 0f, 1f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("잔굴곡", EditorStyles.boldLabel);
        detailHeight = EditorGUILayout.Slider("높이 (m)", detailHeight, 0f, 2f);
        detailScale = EditorGUILayout.Slider("주파수", detailScale, 0.02f, 0.3f);
        SizeHint(detailScale);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("오브젝트 보호", EditorStyles.boldLabel);
        protectObjects = EditorGUILayout.Toggle("큰 오브젝트 주변 평지 유지", protectObjects);
        using (new EditorGUI.DisabledScope(!protectObjects))
        {
            protectMargin = EditorGUILayout.Slider("평지 여유 (m)", protectMargin, 0f, 15f);
            protectFalloff = EditorGUILayout.Slider("경사 전환 거리 (m)", protectFalloff, 1f, 30f);
            minProtectSize = EditorGUILayout.Slider("보호 최소 크기 (m)", minProtectSize, 0.5f, 10f);
            maxProtectSize = EditorGUILayout.Slider("보호 최대 크기 (m)", maxProtectSize, 10f, 300f);
        }
        snapObjects = EditorGUILayout.Toggle("작은 오브젝트 바닥 따라가기", snapObjects);
        using (new EditorGUI.DisabledScope(!snapObjects))
            snapTolerance = EditorGUILayout.Slider("바닥 판정 거리 (m)", snapTolerance, 0.1f, 5f);
        EditorGUILayout.HelpBox(
            "최대 크기보다 큰 콜라이더(경계벽, 트리거 볼륨 등)는 보호에서 뺀다. 넣으면 맵 전체가 평지로 남는다.",
            MessageType.None);

        EditorGUILayout.Space();
        seed = EditorGUILayout.IntField("시드", seed);
        onlySelected = EditorGUILayout.Toggle("선택한 Terrain만", onlySelected);
        backup = EditorGUILayout.Toggle("백업 없으면 실행 전 백업", backup);

        var targets = CollectTargets();
        EditorGUILayout.LabelField($"대상 Terrain: {targets.Count}개");

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(targets.Count == 0))
        {
            if (GUILayout.Button("높낮이 적용", GUILayout.Height(32)))
                ApplyAll(targets);
        }
        EditorGUILayout.HelpBox(
            "적용 후 NavMesh를 다시 구워야 한다. 암반 칠하기가 경사를 쓰므로 스플랫은 이 뒤에 칠한다.",
            MessageType.Warning);

        EditorGUILayout.Space(12);
        EditorGUILayout.LabelField("백업에서 복원", EditorStyles.boldLabel);
        if (restoreSource == null && targets.Count == 1)
            restoreSource = FindOldestBackup(targets[0].terrainData);
        restoreSource = (TerrainData)EditorGUILayout.ObjectField("백업 TerrainData", restoreSource, typeof(TerrainData), false);
        using (new EditorGUI.DisabledScope(restoreSource == null || targets.Count != 1))
        {
            if (GUILayout.Button("높이와 칠하기를 백업 상태로 복원"))
                Restore(targets[0], restoreSource);
        }
        EditorGUILayout.HelpBox("기본값은 가장 오래된 백업(툴을 처음 쓰기 직전 상태)이다. 옮겨진 소품 위치는 되돌리지 않는다.",
            MessageType.None);

        EditorGUILayout.EndScrollView();
    }

    [SerializeField] TerrainData restoreSource;

    static TerrainData FindOldestBackup(TerrainData data)
    {
        string path = AssetDatabase.GetAssetPath(data);
        if (string.IsNullOrEmpty(path)) return null;
        string dir = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string prefix = System.IO.Path.GetFileNameWithoutExtension(path) + "_backup_";
        // 파일명의 시각이 yyyyMMdd_HHmmss라 문자열 정렬이 곧 시간순이다
        string oldest = System.IO.Directory.GetFiles(dir, prefix + "*.asset")
            .Select(p => p.Replace('\\', '/'))
            .OrderBy(p => p, StringComparer.Ordinal)
            .FirstOrDefault();
        return oldest != null ? AssetDatabase.LoadAssetAtPath<TerrainData>(oldest) : null;
    }

    // 파일을 통째로 바꾸면 씬이 참조하는 GUID가 달라지므로, 원본 TerrainData에 값만 복사한다
    static void Restore(Terrain terrain, TerrainData source)
    {
        var data = terrain.terrainData;
        if (source == data) return;
        if (source.heightmapResolution != data.heightmapResolution || source.alphamapResolution != data.alphamapResolution)
        {
            Debug.LogError("[TerrainDune] 백업과 해상도가 달라 복원할 수 없다.");
            return;
        }

        Undo.RegisterCompleteObjectUndo(data, "Restore Terrain Backup");
        int hr = source.heightmapResolution;
        data.SetHeights(0, 0, source.GetHeights(0, 0, hr, hr));
        data.terrainLayers = source.terrainLayers;
        int ar = source.alphamapResolution;
        data.SetAlphamaps(0, 0, source.GetAlphamaps(0, 0, ar, ar));
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        Debug.Log($"[TerrainDune] {AssetDatabase.GetAssetPath(source)}에서 복원 완료");
    }

    static void SizeHint(float noiseScale)
        => EditorGUILayout.LabelField(" ", $"굴곡 하나 약 {1f / noiseScale:F0} m", EditorStyles.miniLabel);

    List<Terrain> CollectTargets()
    {
        if (onlySelected)
            return Selection.gameObjects
                .Select(go => go.GetComponent<Terrain>())
                .Where(t => t != null && t.terrainData != null).ToList();

        return FindObjectsByType<Terrain>(FindObjectsSortMode.None)
            .Where(t => t.terrainData != null).ToList();
    }

    void ApplyAll(List<Terrain> targets)
    {
        var protectedBounds = protectObjects ? CollectProtectedBounds() : new List<Bounds>();
        var snapCandidates = snapObjects ? CollectSnapCandidates(targets) : new List<Transform>();

        // 오브젝트 스냅은 바뀌기 전 높이와 비교해야 하므로 먼저 기록해 둔다
        var before = snapCandidates.ToDictionary(t => t, t => SampleHeight(targets, t.position));

        int group = Undo.GetCurrentGroup();
        try
        {
            for (int i = 0; i < targets.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Terrain Dune", $"{targets[i].name} 적용 중...", (float)i / targets.Count);
                if (backup) Backup(targets[i].terrainData);
                Apply(targets[i], protectedBounds);
            }

            int moved = 0;
            foreach (var t in snapCandidates)
            {
                float? oldH = before[t];
                float? newH = SampleHeight(targets, t.position);
                if (!oldH.HasValue || !newH.HasValue) continue;
                float delta = newH.Value - oldH.Value;
                if (Mathf.Abs(delta) < 0.001f) continue;

                Undo.RecordObject(t, "Snap To Dune");
                t.position += Vector3.up * delta;
                moved++;
            }

            Undo.CollapseUndoOperations(group);
            AssetDatabase.SaveAssets();
            Debug.Log($"[TerrainDune] Terrain {targets.Count}개 적용, 보호 영역 {protectedBounds.Count}개, 바닥 따라 이동 {moved}개");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    void Apply(Terrain terrain, List<Bounds> protectedBounds)
    {
        var data = terrain.terrainData;
        Undo.RegisterCompleteObjectUndo(data, "Terrain Dune Noise");

        int res = data.heightmapResolution;
        float[,] heights = data.GetHeights(0, 0, res, res);
        float[,] mask = BuildMask(terrain, res, protectedBounds);

        Vector3 origin = terrain.transform.position;
        Vector3 size = data.size;
        float offset = seed * 0.7717f;

        for (int z = 0; z < res; z++)
        {
            float wz = origin.z + (float)z / (res - 1) * size.z;
            for (int x = 0; x < res; x++)
            {
                if (mask[z, x] <= 0f) continue;
                float wx = origin.x + (float)x / (res - 1) * size.x;

                float n = Mathf.PerlinNoise(wx * duneScale + offset, wz * duneScale + offset);
                // 뾰족한 능선 모양을 섞어 둥근 언덕만 반복되는 느낌을 줄인다
                float ridged = 1f - Mathf.Abs(n * 2f - 1f);
                float dune = Mathf.Lerp(n, ridged * ridged, ridgeSharpness);
                float detail = Mathf.PerlinNoise(wx * detailScale + offset * 1.9f, wz * detailScale + offset * 1.9f);

                // 올리기만 해서 바닥이 0인 지형에서 음수로 잘리는 일이 없게 한다
                float add = (dune * duneHeight + detail * detailHeight) * mask[z, x];
                heights[z, x] = Mathf.Clamp01(heights[z, x] + add / size.y);
            }
        }

        data.SetHeights(0, 0, heights);
        EditorUtility.SetDirty(data);
    }

    float[,] BuildMask(Terrain terrain, int res, List<Bounds> protectedBounds)
    {
        var mask = new float[res, res];
        for (int z = 0; z < res; z++)
            for (int x = 0; x < res; x++)
                mask[z, x] = 1f;

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float reach = protectMargin + protectFalloff;

        // 보호 영역마다 영향 범위의 샘플만 돌아 전체 격자 × 오브젝트 수만큼 도는 비용을 피한다
        foreach (var b in protectedBounds)
        {
            int x0 = ToIndex(b.min.x - reach, origin.x, size.x, res);
            int x1 = ToIndex(b.max.x + reach, origin.x, size.x, res);
            int z0 = ToIndex(b.min.z - reach, origin.z, size.z, res);
            int z1 = ToIndex(b.max.z + reach, origin.z, size.z, res);

            for (int z = z0; z <= z1; z++)
            {
                float wz = origin.z + (float)z / (res - 1) * size.z;
                float dz = Mathf.Max(b.min.z - wz, 0f, wz - b.max.z);
                for (int x = x0; x <= x1; x++)
                {
                    float wx = origin.x + (float)x / (res - 1) * size.x;
                    float dx = Mathf.Max(b.min.x - wx, 0f, wx - b.max.x);
                    float d = Mathf.Sqrt(dx * dx + dz * dz) - protectMargin;
                    float w = Mathf.SmoothStep(0f, 1f, d / protectFalloff);
                    if (w < mask[z, x]) mask[z, x] = w;
                }
            }
        }
        return mask;
    }

    static int ToIndex(float world, float origin, float size, int res)
        => Mathf.Clamp(Mathf.RoundToInt((world - origin) / size * (res - 1)), 0, res - 1);

    List<Bounds> CollectProtectedBounds()
    {
        return FindObjectsByType<Collider>(FindObjectsSortMode.None)
            .Where(c => c.enabled && !c.isTrigger && c is not TerrainCollider)
            .Select(c => c.bounds)
            .Where(b =>
            {
                float footprint = Mathf.Max(b.size.x, b.size.z);
                return footprint >= minProtectSize && footprint <= maxProtectSize;
            })
            .ToList();
    }

    // 프리팹 인스턴스는 루트째 옮긴다. 메시 자식만 옮기면 루트의 콜라이더·스크립트와 위치가 어긋난다.
    // 프리팹이 아니면 렌더러가 붙은 가장 바깥 오브젝트를 옮겨, 부모와 자식이 두 번 이동하지 않게 한다.
    List<Transform> CollectSnapCandidates(List<Terrain> terrains)
    {
        var roots = new HashSet<Transform>();
        foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
        {
            if (r.GetComponentInParent<Terrain>() != null) continue;

            var prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(r.gameObject);
            Transform top = prefabRoot != null ? prefabRoot.transform : r.transform;
            if (prefabRoot == null)
                for (var p = r.transform.parent; p != null; p = p.parent)
                    if (p.GetComponent<Renderer>() != null || p.GetComponent<LODGroup>() != null) top = p;
            roots.Add(top);
        }

        // 한 루트가 다른 루트의 자식이면 부모만 남긴다
        var nested = roots.Where(t =>
        {
            for (var p = t.parent; p != null; p = p.parent)
                if (roots.Contains(p)) return true;
            return false;
        }).ToList();
        roots.ExceptWith(nested);

        return roots.Where(t =>
        {
            // 맵 전체를 묶은 프리팹처럼 큰 루트는 한 점의 높이 차로 통째 움직이면 안 된다
            if (Footprint(t) > maxProtectSize) return false;
            float? h = SampleHeight(terrains, t.position);
            return h.HasValue && Mathf.Abs(t.position.y - h.Value) <= snapTolerance;
        }).ToList();
    }

    static float Footprint(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return 0f;
        Bounds b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return Mathf.Max(b.size.x, b.size.z);
    }

    static float? SampleHeight(List<Terrain> terrains, Vector3 world)
    {
        foreach (var t in terrains)
        {
            Vector3 local = world - t.transform.position;
            Vector3 size = t.terrainData.size;
            if (local.x < 0 || local.z < 0 || local.x > size.x || local.z > size.z) continue;
            return t.SampleHeight(world) + t.transform.position.y;
        }
        return null;
    }

    // 시드를 바꿔 여러 번 돌려도 원본은 첫 백업 하나면 충분하다. 매번 만들면 9MB씩 쌓인다.
    static void Backup(TerrainData data)
    {
        string path = AssetDatabase.GetAssetPath(data);
        if (string.IsNullOrEmpty(path)) return;
        if (FindOldestBackup(data) != null) return;
        string backupPath = path.Replace(".asset", $"_backup_{DateTime.Now:yyyyMMdd_HHmmss}.asset");
        if (AssetDatabase.CopyAsset(path, backupPath))
            Debug.Log($"[TerrainDune] 백업: {backupPath}");
    }
}
