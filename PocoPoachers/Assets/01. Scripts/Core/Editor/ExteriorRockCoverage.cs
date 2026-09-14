using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExteriorRockCoverage
{
    private const string ScenePath = "Assets/Scenes/02_Raid/SC_Desert_750_Reference.unity";
    private const string Folder = "Assets/GeneratedExteriorRocks";
    private const string Suffix = "_CoverageV2";
    private static string RequestKey => "PocoPoachers.ExteriorRockCoverage.2." + Application.dataPath;

    [InitializeOnLoadMethod]
    private static void Schedule() => EditorApplication.delayCall += ApplyOnce;

    private static void ApplyOnce()
    {
        if (EditorPrefs.GetBool(RequestKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SceneManager.GetActiveScene().path == ScenePath && Apply()) EditorPrefs.SetBool(RequestKey, true);
    }

    [MenuItem("Tools/Terrain/외곽 Rock 크기 확대 및 틈 가리기")]
    private static void ApplyFromMenu()
    {
        if (Apply()) EditorPrefs.SetBool(RequestKey, true);
    }

    private static bool Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || EditorApplication.isPlayingOrWillChangePlaymode) return false;
        var targets = new List<MeshFilter>();
        foreach (var root in scene.GetRootGameObjects())
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            var mesh = filter.sharedMesh;
            if (mesh == null || !filter.name.StartsWith("절벽_Rock")) continue;
            if (!AssetDatabase.GetAssetPath(mesh).StartsWith(Folder + "/") || mesh.name.EndsWith(Suffix)) continue;
            targets.Add(filter);
        }
        if (targets.Count == 0) return false;
        // 원본 메시를 보존하고 확대본을 공유해 씬의 실행 취소가 안전하게 동작하도록 한다.
        var replacements = new Dictionary<Mesh, Mesh>();
        foreach (var filter in targets)
        {
            var source = filter.sharedMesh;
            if (replacements.ContainsKey(source)) continue;
            var mesh = Object.Instantiate(source);
            mesh.name = source.name + Suffix;
            var bounds = source.bounds;
            var scale = new Vector3(1.6f, 1.4f, 1.6f);
            var vertices = source.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                var vertex = vertices[i];
                vertex.x = bounds.center.x + (vertex.x - bounds.center.x) * scale.x;
                vertex.z = bounds.center.z + (vertex.z - bounds.center.z) * scale.z;
                // 밑부분을 약간 묻어 하단 틈을 줄이면서 윗부분을 높인다.
                vertex.y = bounds.min.y + (vertex.y - bounds.min.y) * scale.y - bounds.size.y * 0.12f;
                vertices[i] = vertex;
            }
            mesh.vertices = vertices;
            var normals = source.normals;
            for (int i = 0; i < normals.Length; i++)
                normals[i] = new Vector3(normals[i].x / scale.x, normals[i].y / scale.y, normals[i].z / scale.z).normalized;
            if (normals.Length == vertices.Length) mesh.normals = normals;
            else mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            AssetDatabase.CreateAsset(mesh, AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + mesh.name + ".asset"));
            AssetDatabase.SaveAssetIfDirty(mesh);
            replacements.Add(source, mesh);
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("외곽 바위 확대 및 틈 가리기");
        foreach (var filter in targets)
        {
            Undo.RecordObject(filter, "외곽 바위 확대");
            filter.sharedMesh = replacements[filter.sharedMesh];
            PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        SceneView.RepaintAll();
        Debug.Log($"[ExteriorRockCoverage] {targets.Count}개 확대 완료: 폭/두께 1.6배, 높이 1.4배, 하단 12% 매립. Ctrl+Z로 복원할 수 있습니다.");
        return true;
    }
}
