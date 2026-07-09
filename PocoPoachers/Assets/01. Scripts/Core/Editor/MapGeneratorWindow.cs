using System;
using System.IO;
using System.Linq;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

// 이미지 레이어(Height/Biome/Object/Resource/Enemy Map)를 읽어 Terrain과 프리팹,
// NavMesh를 자동 생성하는 에디터 툴. map_plan/ 제안서의 파이프라인을 구현한다.
public class MapGeneratorWindow : EditorWindow
{
    const string RootName = "GeneratedMap";
    const string GeneratedAssetDir = "Assets/_Generated/MapGen";

    // 레이어 텍스처
    Texture2D heightMap;
    Texture2D biomeMap;
    Texture2D objectMap;
    Texture2D resourceMap;
    Texture2D enemySpawnMap;

    MapLayerPalette palette;

    // 지형 설정
    float terrainWidth = 512f;
    float terrainLength = 512f;
    float terrainHeight = 80f;
    int heightmapResolution = 513;

    // 배치 격자 간격 (월드 유닛)
    float objectGridStep = 4f;
    float resourceGridStep = 6f;
    float enemyGridStep = 8f;
    float enemyBrightnessThreshold = 0.5f;

    Vector2 scroll;

    [MenuItem("Tools/Generator/Map")]
    public static void Open() => GetWindow<MapGeneratorWindow>("Map Generator");

    void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        EditorGUILayout.LabelField("레이어 텍스처", EditorStyles.boldLabel);
        heightMap = (Texture2D)EditorGUILayout.ObjectField("Height Map (필수)", heightMap, typeof(Texture2D), false);
        biomeMap = (Texture2D)EditorGUILayout.ObjectField("Biome Map", biomeMap, typeof(Texture2D), false);
        objectMap = (Texture2D)EditorGUILayout.ObjectField("Object Map", objectMap, typeof(Texture2D), false);
        resourceMap = (Texture2D)EditorGUILayout.ObjectField("Resource Map", resourceMap, typeof(Texture2D), false);
        enemySpawnMap = (Texture2D)EditorGUILayout.ObjectField("Enemy Spawn Map", enemySpawnMap, typeof(Texture2D), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("색상 팔레트", EditorStyles.boldLabel);
        palette = (MapLayerPalette)EditorGUILayout.ObjectField("Layer Palette (필수)", palette, typeof(MapLayerPalette), false);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("지형 설정", EditorStyles.boldLabel);
        terrainWidth = EditorGUILayout.FloatField("Width (X)", terrainWidth);
        terrainLength = EditorGUILayout.FloatField("Length (Z)", terrainLength);
        terrainHeight = EditorGUILayout.FloatField("Max Height (Y)", terrainHeight);
        heightmapResolution = EditorGUILayout.IntField("Heightmap Resolution", heightmapResolution);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("배치 격자 간격 (m)", EditorStyles.boldLabel);
        objectGridStep = EditorGUILayout.FloatField("Object Grid Step", objectGridStep);
        resourceGridStep = EditorGUILayout.FloatField("Resource Grid Step", resourceGridStep);
        enemyGridStep = EditorGUILayout.FloatField("Enemy Grid Step", enemyGridStep);
        enemyBrightnessThreshold = EditorGUILayout.Slider("Enemy Spawn 밝기 임계값", enemyBrightnessThreshold, 0f, 1f);

        EditorGUILayout.Space(12);
        bool canGenerate = CanGenerate(out var reason);
        using (new EditorGUI.DisabledScope(!canGenerate))
        {
            if (GUILayout.Button("맵 생성", GUILayout.Height(32)))
                TryGenerate();
        }
        if (!canGenerate)
            EditorGUILayout.HelpBox(reason, MessageType.Warning);

        EditorGUILayout.EndScrollView();
    }

    bool CanGenerate(out string reason)
    {
        if (heightMap == null) { reason = "Height Map을 지정해주세요."; return false; }
        if (palette == null) { reason = "Layer Palette를 지정해주세요."; return false; }

        foreach (var (tex, label) in new[]
                 {
                     (heightMap, "Height Map"), (biomeMap, "Biome Map"), (objectMap, "Object Map"),
                     (resourceMap, "Resource Map"), (enemySpawnMap, "Enemy Spawn Map"),
                 })
        {
            if (tex != null && !tex.isReadable)
            {
                reason = $"{label} 텍스처의 Import Settings에서 Read/Write Enabled를 켜주세요.";
                return false;
            }
        }

        reason = null;
        return true;
    }

    void TryGenerate()
    {
        var existing = GameObject.Find(RootName);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Map Generator",
                    $"씬에 이미 '{RootName}'이 있습니다. 삭제하고 새로 생성할까요?", "계속", "취소"))
                return;
            Undo.DestroyObjectImmediate(existing);
        }

        var root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Generate Map");

        try
        {
            Directory.CreateDirectory(GeneratedAssetDir);

            EditorUtility.DisplayProgressBar("Map Generator", "지형(Height Map) 생성 중...", 0.1f);
            var terrain = GenerateTerrain(root.transform);

            if (biomeMap != null)
            {
                EditorUtility.DisplayProgressBar("Map Generator", "바이옴 페인팅 중...", 0.3f);
                PaintBiomes(terrain.terrainData);
            }

            if (objectMap != null)
            {
                EditorUtility.DisplayProgressBar("Map Generator", "Object Map 배치 중...", 0.45f);
                PlacePrefabs(terrain, root.transform, "Objects", objectMap, palette.MatchObject, objectGridStep);
            }

            if (resourceMap != null)
            {
                EditorUtility.DisplayProgressBar("Map Generator", "Resource Map 배치 중...", 0.6f);
                PlacePrefabs(terrain, root.transform, "Resources", resourceMap, palette.MatchResource, resourceGridStep);
            }

            EditorUtility.DisplayProgressBar("Map Generator", "NavMesh 베이크 중...", 0.8f);
            BuildNavMesh(root);

            if (enemySpawnMap != null)
            {
                EditorUtility.DisplayProgressBar("Map Generator", "Enemy Spawn Map 배치 중...", 0.9f);
                GenerateSpawnPoints(terrain, root.transform);
            }

            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog("Map Generator", "맵 생성이 완료되었습니다.", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    // ------------------------------------------------------------------
    // Height Map → Terrain
    // ------------------------------------------------------------------
    Terrain GenerateTerrain(Transform parent)
    {
        var go = new GameObject("Terrain");
        go.transform.SetParent(parent);
        var terrain = go.AddComponent<Terrain>();
        var collider = go.AddComponent<TerrainCollider>();

        // AddComponent로 생성한 Terrain은 materialTemplate이 비어있어 URP에서 핑크로 렌더링됨.
        // 에디터 메뉴로 생성할 때와 달리 자동으로 URP Terrain 셰이더가 할당되지 않으므로 직접 만들어 지정한다.
        terrain.materialTemplate = GetOrCreateTerrainMaterial();

        string dataPath = $"{GeneratedAssetDir}/GeneratedTerrainData.asset";
        var data = AssetDatabase.LoadAssetAtPath<TerrainData>(dataPath);
        bool isNew = data == null;
        if (isNew) data = new TerrainData();

        data.heightmapResolution = heightmapResolution;
        data.size = new Vector3(terrainWidth, terrainHeight, terrainLength);

        int res = data.heightmapResolution;
        var heights = new float[res, res];
        for (int y = 0; y < res; y++)
        {
            float v = (float)y / (res - 1);
            for (int x = 0; x < res; x++)
            {
                float u = (float)x / (res - 1);
                heights[y, x] = heightMap.GetPixelBilinear(u, v).grayscale;
            }
        }
        data.SetHeights(0, 0, heights);

        if (isNew) AssetDatabase.CreateAsset(data, dataPath);
        else EditorUtility.SetDirty(data);

        terrain.terrainData = data;
        collider.terrainData = data;
        return terrain;
    }

    static Material GetOrCreateTerrainMaterial()
    {
        string path = $"{GeneratedAssetDir}/GeneratedTerrainMaterial.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        var shader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (shader == null)
        {
            Debug.LogWarning("[MapGenerator] URP Terrain 셰이더를 찾지 못했습니다. Terrain 기본 머티리얼을 사용합니다.");
            return null;
        }

        mat = new Material(shader);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    // ------------------------------------------------------------------
    // Biome Map → TerrainLayer 알파맵
    // ------------------------------------------------------------------
    void PaintBiomes(TerrainData data)
    {
        var layers = palette.biomes.Select(b => b.terrainLayer).Where(l => l != null).Distinct().ToArray();
        if (layers.Length == 0) return; // 팔레트에 TerrainLayer가 지정되지 않았으면 스킵

        data.terrainLayers = layers;

        // 새로 만든 TerrainData는 alphamapResolution이 기본값 0이라 명시적으로 설정해야 함
        if (data.alphamapResolution <= 0) data.alphamapResolution = 512;

        int alphaRes = data.alphamapResolution;
        var alphamaps = new float[alphaRes, alphaRes, layers.Length];

        for (int y = 0; y < alphaRes; y++)
        {
            float v = (float)y / (alphaRes - 1);
            for (int x = 0; x < alphaRes; x++)
            {
                float u = (float)x / (alphaRes - 1);
                var pixel = biomeMap.GetPixelBilinear(u, v);
                var biome = palette.MatchBiome(pixel);
                int layerIndex = biome != null ? Array.IndexOf(layers, biome.terrainLayer) : -1;
                alphamaps[y, x, layerIndex >= 0 ? layerIndex : 0] = 1f;
            }
        }
        data.SetAlphamaps(0, 0, alphamaps);
    }

    // ------------------------------------------------------------------
    // Object Map / Resource Map → 프리팹 배치
    // ------------------------------------------------------------------
    void PlacePrefabs(Terrain terrain, Transform parent, string containerName, Texture2D map,
        Func<Color, MapLayerPalette.PlacementEntry> matcher, float gridStep)
    {
        var container = new GameObject(containerName).transform;
        container.SetParent(parent);
        container.localPosition = Vector3.zero;

        var data = terrain.terrainData;
        int stepsX = Mathf.Max(1, Mathf.RoundToInt(data.size.x / gridStep));
        int stepsZ = Mathf.Max(1, Mathf.RoundToInt(data.size.z / gridStep));

        for (int zi = 0; zi < stepsZ; zi++)
        {
            float v = (zi + 0.5f) / stepsZ;
            for (int xi = 0; xi < stepsX; xi++)
            {
                float u = (xi + 0.5f) / stepsX;
                var pixel = map.GetPixelBilinear(u, v);
                var entry = matcher(pixel);
                if (entry?.prefab == null) continue;
                if (UnityEngine.Random.value > entry.density) continue;

                float worldX = u * data.size.x;
                float worldZ = v * data.size.z;
                var basePos = terrain.transform.position + new Vector3(worldX, 0f, worldZ);
                basePos.y = terrain.SampleHeight(basePos) + terrain.transform.position.y;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(entry.prefab, container);
                instance.transform.position = basePos;
                if (entry.randomYRotation)
                    instance.transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
                float scale = UnityEngine.Random.Range(entry.uniformScaleRange.x, entry.uniformScaleRange.y);
                instance.transform.localScale *= scale;
            }
        }
    }

    // ------------------------------------------------------------------
    // NavMesh 베이크
    // ------------------------------------------------------------------
    void BuildNavMesh(GameObject root)
    {
        var surface = root.GetComponent<NavMeshSurface>();
        if (surface == null) surface = root.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.BuildNavMesh();
    }

    // ------------------------------------------------------------------
    // Enemy Spawn Map → 스폰 포인트 (NavMesh 유효 지점만)
    // ------------------------------------------------------------------
    void GenerateSpawnPoints(Terrain terrain, Transform parent)
    {
        var container = new GameObject("EnemySpawnPoints").transform;
        container.SetParent(parent);
        container.localPosition = Vector3.zero;

        var data = terrain.terrainData;
        int stepsX = Mathf.Max(1, Mathf.RoundToInt(data.size.x / enemyGridStep));
        int stepsZ = Mathf.Max(1, Mathf.RoundToInt(data.size.z / enemyGridStep));

        int count = 0;
        for (int zi = 0; zi < stepsZ; zi++)
        {
            float v = (zi + 0.5f) / stepsZ;
            for (int xi = 0; xi < stepsX; xi++)
            {
                float u = (xi + 0.5f) / stepsX;
                float brightness = enemySpawnMap.GetPixelBilinear(u, v).grayscale;
                if (brightness < enemyBrightnessThreshold) continue;

                float worldX = u * data.size.x;
                float worldZ = v * data.size.z;
                var worldPos = terrain.transform.position + new Vector3(worldX, 0f, worldZ);
                worldPos.y = terrain.SampleHeight(worldPos) + terrain.transform.position.y;

                if (!NavMesh.SamplePosition(worldPos, out var hit, enemyGridStep, NavMesh.AllAreas))
                    continue;

                var point = new GameObject($"SpawnPoint_{count++}");
                point.transform.SetParent(container);
                point.transform.position = hit.position;
            }
        }
    }
}
