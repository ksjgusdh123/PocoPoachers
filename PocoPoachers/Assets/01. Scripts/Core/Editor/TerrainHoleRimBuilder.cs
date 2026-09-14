using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

public static class TerrainHoleRimBuilder
{
    private const string RootName = "TerrainHoleRim";
    private const string TrialKey = "PocoPoachers.HoleRim.Trial.1";
    private const string RockPath = "Assets/_Art/Environment/Textures/Terrain/Rock.terrainlayer";

    [InitializeOnLoadMethod]
    private static void ScheduleRockUpdate() => EditorApplication.delayCall += UpdateExistingRock;

    private static void UpdateExistingRock()
    {
        const string folder = "Assets/GeneratedTerrainRims";
        var rock = AssetDatabase.LoadAssetAtPath<TerrainLayer>(RockPath);
        if (rock == null || !AssetDatabase.IsValidFolder(folder)) return;
        foreach (string guid in AssetDatabase.FindAssets("", new[] { folder }))
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(guid)))
        {
            if (!(asset is Material material) || material.name != "경계 흙 재질") continue;
            Undo.RecordObject(material, "경계 마감을 Rock으로 변경");
            ApplyRock(material, rock);
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssetIfDirty(material);
        }
        SceneView.RepaintAll();
    }

    private static void ApplyRock(Material material, TerrainLayer rock)
    {
        material.name = "경계 Rock 재질";
        material.SetColor("_BaseColor", Color.white);
        material.SetTexture("_BaseMap", rock.diffuseTexture);
        var scale = new Vector2(1f / Mathf.Max(0.01f, rock.tileSize.x), 1f / Mathf.Max(0.01f, rock.tileSize.y));
        material.SetTextureScale("_BaseMap", scale);
        material.SetTextureOffset("_BaseMap", Vector2.Scale(rock.tileOffset, scale));
        material.SetFloat("_Smoothness", rock.smoothness);
        material.SetFloat("_Metallic", rock.metallic);
        material.SetTexture("_BumpMap", rock.normalMapTexture);
        material.SetFloat("_BumpScale", rock.normalScale);
        if (rock.normalMapTexture != null) material.EnableKeyword("_NORMALMAP");
        else material.DisableKeyword("_NORMALMAP");
    }

    // 이번 요청의 시험 적용만 열린 대상 씬에서 한 번 실행한다. 씬은 자동 저장하지 않는다.
    [InitializeOnLoadMethod]
    private static void ScheduleTrial() => EditorApplication.delayCall += TryTrial;

    private static void TryTrial()
    {
        string trialKey = TrialKey + Application.dataPath;
        if (EditorPrefs.GetBool(trialKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        var scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/02_Raid/SC_Desert_750_Reference.unity") return;
        foreach (var root in scene.GetRootGameObjects())
        foreach (var terrain in root.GetComponentsInChildren<Terrain>())
        {
            if (terrain.terrainData == null || terrain.transform.Find(RootName) != null) continue;
            EditorPrefs.SetBool(trialKey, true);
            Build(terrain, 3f);
            return;
        }
    }

    public static void Build(Terrain terrain, float depth)
    {
        if (terrain == null || terrain.terrainData == null) return;
        var data = terrain.terrainData;
        int n = data.holesResolution;
        var holes = data.GetHoles(0, 0, n, n);
        var next = new Dictionary<Vector2Int, List<Vector2Int>>();
        void Edge(int ax, int az, int bx, int bz)
        {
            var a = new Vector2Int(ax, az);
            if (!next.TryGetValue(a, out var list)) next[a] = list = new List<Vector2Int>();
            list.Add(new Vector2Int(bx, bz));
        }
        // 모든 경계의 진행 방향을 통일해 왼쪽이 지면이 되도록 한다.
        for (int z = 0; z < n; z++)
        for (int x = 0; x < n; x++)
        {
            if (!holes[z, x]) continue;
            if (z > 0 && !holes[z - 1, x]) Edge(x, z, x + 1, z);
            if (x < n - 1 && !holes[z, x + 1]) Edge(x + 1, z, x + 1, z + 1);
            if (z < n - 1 && !holes[z + 1, x]) Edge(x + 1, z + 1, x, z + 1);
            if (x > 0 && !holes[z, x - 1]) Edge(x, z + 1, x, z);
        }
        if (next.Count == 0)
        {
            Debug.LogWarning("구멍 경계가 없습니다. 구멍을 만든 후 마감을 생성하세요.", terrain);
            return;
        }
        float cellX = data.size.x / n, cellZ = data.size.z / n;
        float width = Mathf.Max(cellX, cellZ);
        var vertices = new List<Vector3>();
        var uv = new List<Vector2>();
        var triangles = new List<int>();
        while (next.Count > 0)
        {
            Vector2Int start = default;
            foreach (var key in next.Keys) { start = key; break; }
            var points = new List<Vector2>();
            var current = start;
            bool closed = false;
            while (true)
            {
                points.Add(new Vector2(current.x * cellX, current.y * cellZ));
                if (!next.TryGetValue(current, out var list)) break;
                var end = list[list.Count - 1];
                list.RemoveAt(list.Count - 1);
                if (list.Count == 0) next.Remove(current);
                current = end;
                if (current == start) { closed = true; break; }
            }
            if (points.Count < 2) continue;
            // 원래 윤곽에서 크게 벗어나지 않는 범위로 격자의 모서리를 둥글게 한다.
            for (int pass = 0; pass < 3; pass++)
            {
                var copy = points.ToArray();
                for (int i = closed ? 0 : 1; i < (closed ? points.Count : points.Count - 1); i++)
                    points[i] = copy[i] * 0.5f + (copy[(i + points.Count - 1) % points.Count]
                        + copy[(i + 1) % points.Count]) * 0.25f;
            }
            int offset = vertices.Count;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 before = points[closed ? (i + points.Count - 1) % points.Count : Mathf.Max(0, i - 1)];
                Vector2 after = points[closed ? (i + 1) % points.Count : Mathf.Min(points.Count - 1, i + 1)];
                Vector2 tangent = (after - before).normalized;
                Vector2 normal = new Vector2(-tangent.y, tangent.x);
                Vector2 inner = points[i] + normal * width * 1.5f;
                Vector2 outer = points[i] - normal * width * 1.2f;
                float innerY = Height(inner), outerY = Height(outer);
                vertices.Add(new Vector3(inner.x, innerY, inner.y));
                vertices.Add(new Vector3(outer.x, outerY, outer.y));
                vertices.Add(new Vector3(outer.x, outerY - depth, outer.y));
                uv.Add(inner); uv.Add(outer); uv.Add(outer + normal * depth);
            }
            int segments = closed ? points.Count : points.Count - 1;
            for (int i = 0; i < segments; i++)
            {
                int a = offset + i * 3, b = offset + ((i + 1) % points.Count) * 3;
                triangles.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1,
                    a + 1, b + 1, a + 2, a + 2, b + 1, b + 2 });
            }
        }
        float Height(Vector2 p) => data.GetInterpolatedHeight(
            Mathf.Clamp01(p.x / data.size.x), Mathf.Clamp01(p.y / data.size.z)) + 0.06f;

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) { Debug.LogError("URP Lit 셰이더를 찾을 수 없습니다."); return; }
        var mesh = new Mesh { name = "구멍 경계 마감", indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(vertices); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals(); mesh.RecalculateBounds();
        var material = new Material(shader) { name = "경계 흙 재질" };
        material.SetFloat("_Smoothness", 0f);
        material.SetFloat("_Cull", 0f);
        var layers = data.terrainLayers;
        if (layers.Length > 0 && layers[0] != null && layers[0].diffuseTexture != null)
        {
            var layer = layers[0];
            material.SetTexture("_BaseMap", layer.diffuseTexture);
            material.SetTextureScale("_BaseMap", new Vector2(1f / Mathf.Max(0.01f, layer.tileSize.x), 1f / Mathf.Max(0.01f, layer.tileSize.y)));
            material.SetTextureOffset("_BaseMap", new Vector2(layer.tileOffset.x / Mathf.Max(0.01f, layer.tileSize.x), layer.tileOffset.y / Mathf.Max(0.01f, layer.tileSize.y)));
        }
        else material.SetColor("_BaseColor", new Color(0.55f, 0.44f, 0.28f));
        var rock = AssetDatabase.LoadAssetAtPath<TerrainLayer>(RockPath);
        if (rock != null) ApplyRock(material, rock);
        const string folder = "Assets/GeneratedTerrainRims";
        if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder("Assets", "GeneratedTerrainRims");
        string assetPath = AssetDatabase.GenerateUniqueAssetPath(folder + "/TerrainHoleRim.asset");
        AssetDatabase.CreateAsset(mesh, assetPath);
        AssetDatabase.AddObjectToAsset(material, mesh);
        AssetDatabase.SaveAssetIfDirty(mesh);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("구멍 경계 마감 생성");
        var old = terrain.transform.Find(RootName);
        if (old != null) Undo.DestroyObjectImmediate(old.gameObject);
        var go = new GameObject(RootName);
        go.transform.SetParent(terrain.transform, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        Undo.RegisterCreatedObjectUndo(go, "구멍 경계 마감 생성");
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
        Selection.activeGameObject = go;
        SceneView.RepaintAll();
        Debug.Log("TerrainHoleRim을 생성했습니다. 활성화를 껐다 켜서 비교하세요. 시각용 마감이며 충돌은 변경하지 않습니다.", go);
    }
}
