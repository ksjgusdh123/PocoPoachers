using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExteriorRockMixer
{
    private const string ScenePath = "Assets/Scenes/02_Raid/SC_Raid_1001.unity";
    private const string SourceFolder = "Assets/_External/AI/Prefabs/Environments/Desert/Rocks/";
    private const string OriginalFolder = "Assets/_External/AI/Models/Environments/Desert/DesertReference750/";
    private const string OutputFolder = "Assets/GeneratedExteriorRocks";
    private static string RequestKey => "PocoPoachers.ExteriorRockMix.20260915.1." + Application.dataPath;

    // 이번 교체 요청을 열린 씬에 한 번 적용한다. 사용자의 씬은 자동 저장하지 않는다.
    [InitializeOnLoadMethod]
    private static void Schedule() => EditorApplication.delayCall += ApplyOnce;

    private static void ApplyOnce()
    {
        if (EditorPrefs.GetBool(RequestKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (SceneManager.GetActiveScene().path != ScenePath) return;
        if (Apply()) EditorPrefs.SetBool(RequestKey, true);
    }

    [MenuItem("Tools/Terrain/외곽 층리바위를 Rock1·Rock2로 교체")]
    private static void ApplyFromMenu()
    {
        if (Apply()) EditorPrefs.SetBool(RequestKey, true);
    }

    private static bool Apply()
    {
        var scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode || scene.path != ScenePath)
        {
            Debug.LogWarning("편집 모드에서 SC_Raid_1001 씬을 열어주세요.");
            return false;
        }
        var targets = new List<MeshFilter>();
        foreach (var root in scene.GetRootGameObjects())
        foreach (var filter in root.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!filter.name.StartsWith("절벽_층리바위", StringComparison.Ordinal) || filter.sharedMesh == null) continue;
            if (!AssetDatabase.GetAssetPath(filter.sharedMesh).StartsWith(OriginalFolder, StringComparison.Ordinal)) continue;
            if (filter.GetComponent<MeshRenderer>() != null) targets.Add(filter);
        }
        if (targets.Count == 0)
        {
            Debug.Log("교체할 외곽 층리바위가 없습니다. 이미 교체된 바위는 건너뜁니다.");
            return false;
        }
        var prefabs = new GameObject[2];
        var sources = new MeshFilter[2];
        var materials = new Material[2][];
        var matrices = new Matrix4x4[2];
        for (int i = 0; i < 2; i++)
        {
            prefabs[i] = AssetDatabase.LoadAssetAtPath<GameObject>(SourceFolder + $"Rock{i + 1}.prefab");
            if (prefabs[i] == null) { Debug.LogError("Rock 프리팹을 찾을 수 없습니다."); return false; }
            var filters = prefabs[i].GetComponentsInChildren<MeshFilter>(true);
            if (filters.Length != 1 || filters[0].sharedMesh == null
                || filters[0].GetComponent<MeshRenderer>() == null)
            { Debug.LogError("Rock 프리팹의 메시 구성을 확인하세요."); return false; }
            sources[i] = filters[0];
            materials[i] = filters[0].GetComponent<MeshRenderer>().sharedMaterials;
            // 프리팹의 기본 방향을 유지하되 배치용 위치는 제외한다.
            var root = prefabs[i].transform;
            matrices[i] = Matrix4x4.TRS(Vector3.zero, root.localRotation, root.localScale)
                * root.worldToLocalMatrix * filters[0].transform.localToWorldMatrix;
        }

        // 전역 난수 상태를 바꾸지 않고 종류가 한쪽에 몰리지 않게 섞는다.
        var random = new System.Random(916);
        for (int i = targets.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            var swap = targets[i]; targets[i] = targets[j]; targets[j] = swap;
        }
        if (!AssetDatabase.IsValidFolder(OutputFolder)) AssetDatabase.CreateFolder("Assets", "GeneratedExteriorRocks");
        var cache = new Dictionary<(Mesh, int), Mesh>();
        var replacements = new Mesh[targets.Count];
        // 모든 메시를 준비한 뒤 씬을 변경해 준비 실패 시 부분 교체를 피한다.
        for (int i = 0; i < targets.Count; i++)
        {
            int kind = i % 2;
            var original = targets[i].sharedMesh;
            var key = (original, kind);
            if (!cache.TryGetValue(key, out var mesh))
            {
                mesh = FitMesh(sources[kind].sharedMesh, matrices[kind], original.bounds);
                mesh.name = $"Exterior_Rock{kind + 1}_{original.name}";
                AssetDatabase.CreateAsset(mesh, AssetDatabase.GenerateUniqueAssetPath(OutputFolder + "/" + mesh.name + ".asset"));
                AssetDatabase.SaveAssetIfDirty(mesh);
                cache.Add(key, mesh);
            }
            replacements[i] = mesh;
        }
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("외곽 바위를 Rock1·Rock2로 교체");
        for (int i = 0; i < targets.Count; i++)
        {
            var filter = targets[i];
            var renderer = filter.GetComponent<MeshRenderer>();
            Undo.RecordObjects(new UnityEngine.Object[] { filter, renderer, filter.gameObject }, "외곽 바위 교체");
            filter.sharedMesh = replacements[i];
            renderer.sharedMaterials = materials[i % 2];
            filter.name = "절벽_Rock" + (i % 2 + 1);
            // 기존의 단순 충돌 경계와 배치 Transform은 유지한다.
            PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
            PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(filter.gameObject);
        }
        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(scene);
        SceneView.RepaintAll();
        Debug.Log($"[ExteriorRockMixer] 교체 완료: Rock1 {(targets.Count + 1) / 2}개, Rock2 {targets.Count / 2}개. Ctrl+Z로 되돌릴 수 있습니다.");
        return true;
    }

    private static Mesh FitMesh(Mesh source, Matrix4x4 orientation, Bounds target)
    {
        var vertices = source.vertices;
        if (vertices.Length == 0) throw new InvalidOperationException("Rock 메시가 비어 있습니다.");
        var bounds = new Bounds(orientation.MultiplyPoint3x4(vertices[0]), Vector3.zero);
        foreach (var vertex in vertices) bounds.Encapsulate(orientation.MultiplyPoint3x4(vertex));
        if (bounds.size.x < 0.000001f || bounds.size.y < 0.000001f || bounds.size.z < 0.000001f)
            throw new InvalidOperationException("Rock 메시 크기가 올바르지 않습니다.");
        var scale = new Vector3(target.size.x / bounds.size.x, target.size.y / bounds.size.y, target.size.z / bounds.size.z);
        var fit = Matrix4x4.Translate(target.center) * Matrix4x4.Scale(scale)
            * Matrix4x4.Translate(-bounds.center) * orientation;
        for (int i = 0; i < vertices.Length; i++) vertices[i] = fit.MultiplyPoint3x4(vertices[i]);
        var mesh = UnityEngine.Object.Instantiate(source);
        mesh.vertices = vertices;
        var normals = source.normals;
        var normalMatrix = fit.inverse.transpose;
        for (int i = 0; i < normals.Length; i++) normals[i] = normalMatrix.MultiplyVector(normals[i]).normalized;
        if (normals.Length == vertices.Length) mesh.normals = normals;
        else mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }
}
