using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExteriorRockDepth
{
    private const string ScenePath = "Assets/Scenes/02_Raid/SC_Raid_1001.unity";
    private const string Folder = "Assets/GeneratedExteriorRocks";
    private static string RequestKey => "PocoPoachers.ExteriorRockDepth.3." + Application.dataPath;

    [InitializeOnLoadMethod]
    private static void Schedule() => EditorApplication.delayCall += ApplyOnce;

    private static void ApplyOnce()
    {
        if (!EditorPrefs.GetBool(RequestKey, false) && Apply()) EditorPrefs.SetBool(RequestKey, true);
    }

    [MenuItem("Tools/Terrain/외곽 Rock 하단을 터레인 높이 3까지 연장")]
    private static void ApplyFromMenu()
    {
        if (Apply()) EditorPrefs.SetBool(RequestKey, true);
    }

    private static bool Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath) return false;
        var terrains = new List<Terrain>();
        var filters = new List<MeshFilter>();
        foreach (var root in scene.GetRootGameObjects())
        {
            terrains.AddRange(root.GetComponentsInChildren<Terrain>());
            filters.AddRange(root.GetComponentsInChildren<MeshFilter>(true));
        }
        var changes = new List<(MeshFilter filter, Mesh mesh)>();
        int unchanged = 0;
        foreach (var filter in filters)
        {
            var source = filter.sharedMesh;
            if (source == null || !filter.name.StartsWith("절벽_Rock")) continue;
            if (!AssetDatabase.GetAssetPath(source).StartsWith(Folder + "/")) continue;
            var vertices = source.vertices;
            var toWorld = filter.transform.localToWorldMatrix;
            var toLocal = filter.transform.worldToLocalMatrix;
            if (Mathf.Abs(toWorld.determinant) < 0.0000001f) continue;
            Vector3 center = toWorld.MultiplyPoint3x4(source.bounds.center);
            Terrain terrain = null;
            foreach (var candidate in terrains)
            {
                if (candidate.terrainData == null) continue;
                Vector3 position = candidate.transform.position;
                Vector3 size = candidate.terrainData.size;
                if (center.x >= position.x && center.x <= position.x + size.x
                    && center.z >= position.z && center.z <= position.z + size.z)
                { terrain = candidate; break; }
            }
            if (terrain == null) continue;
            // 지표면 샘플 높이가 아닌 Terrain 원점을 기준으로 한 높이 3이다.
            float targetBottom = terrain.transform.position.y + 3f;
            float bottom = float.MaxValue, top = float.MinValue;
            foreach (var vertex in vertices)
            {
                float y = toWorld.MultiplyPoint3x4(vertex).y;
                bottom = Mathf.Min(bottom, y);
                top = Mathf.Max(top, y);
            }
            if (bottom <= targetBottom + 0.01f || top - bottom < 0.001f)
            { unchanged++; continue; }
            float stretch = (top - targetBottom) / (top - bottom);
            var worldStretch = Matrix4x4.Translate(new Vector3(0, top, 0))
                * Matrix4x4.Scale(new Vector3(1, stretch, 1))
                * Matrix4x4.Translate(new Vector3(0, -top, 0));
            var deformation = toLocal * worldStretch * toWorld;
            var normalMatrix = deformation.inverse.transpose;
            for (int i = 0; i < vertices.Length; i++) vertices[i] = deformation.MultiplyPoint3x4(vertices[i]);
            var mesh = Object.Instantiate(source);
            mesh.name = "Depth3_" + changes.Count + "_" + source.name;
            mesh.vertices = vertices;
            var normals = source.normals;
            for (int i = 0; i < normals.Length; i++) normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
            if (normals.Length == vertices.Length) mesh.normals = normals;
            else mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            changes.Add((filter, mesh));
        }
        if (changes.Count == 0)
        {
            Debug.Log($"[ExteriorRockDepth] 추가 연장 대상 없음. 이미 높이 3 이하: {unchanged}개.");
            return unchanged > 0;
        }
        // 확대본을 별도 에셋으로 보존해 이전 모양으로 실행 취소할 수 있게 한다.
        string path = AssetDatabase.GenerateUniqueAssetPath(Folder + "/ExteriorRockDepth3.asset");
        AssetDatabase.CreateAsset(changes[0].mesh, path);
        for (int i = 1; i < changes.Count; i++) AssetDatabase.AddObjectToAsset(changes[i].mesh, path);
        foreach (var change in changes) EditorUtility.SetDirty(change.mesh);
        AssetDatabase.SaveAssetIfDirty(changes[0].mesh);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("외곽 Rock 하단 연장");
        foreach (var change in changes)
        {
            Undo.RecordObject(change.filter, "바위 하단을 높이 3까지 연장");
            change.filter.sharedMesh = change.mesh;
            PrefabUtility.RecordPrefabInstancePropertyModifications(change.filter);
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        SceneView.RepaintAll();
        Debug.Log($"[ExteriorRockDepth] {changes.Count}개 하단을 Terrain 원점 + 3까지 연장. 이미 낮은 {unchanged}개 유지. 상단과 가로 폭 유지.");
        return true;
    }
}
