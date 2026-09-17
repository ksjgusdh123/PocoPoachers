using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// 프리팹 원본에는 ID를 쓰지 않고 씬 인스턴스에만 저장한다.
[InitializeOnLoad]
public static class SceneItemBoxIdUtility
{
    static SceneItemBoxIdUtility()
    {
        EditorSceneManager.sceneSaving += OnSceneSaving;
    }

    private static void OnSceneSaving(Scene scene, string path)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        Repair(scene);
    }

    public static int Repair(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded || EditorSceneManager.IsPreviewScene(scene)
            || PrefabStageUtility.GetCurrentPrefabStage()?.scene == scene) return 0;

        var boxes = new List<ItemBox>();
        foreach (var root in scene.GetRootGameObjects())
            foreach (var box in root.GetComponentsInChildren<ItemBox>(true))
                if (!(box is LootBox)) boxes.Add(box);

        // 원본 소유자가 일치하는 박스를 먼저 처리해 복제본보다 기존 ID를 우선 보존한다.
        boxes.Sort((a, b) => IsOriginal(b).CompareTo(IsOriginal(a)));
        var reserved = new HashSet<int>();
        foreach (var box in boxes)
        {
            var identity = box;
            if (identity != null && identity.HasValidSceneBoxId) reserved.Add(identity.SceneBoxId);
        }

        int changed = 0;
        var seen = new HashSet<int>();
        foreach (var box in boxes)
        {
            var identity = box;


            string owner = GlobalObjectId.GetGlobalObjectIdSlow(box).ToString();
            int id = identity.SceneBoxId;
            bool copied = !string.IsNullOrEmpty(identity.SceneBoxEditorOwner) && identity.SceneBoxEditorOwner != owner;
            if (id >= 0 || copied || !seen.Add(id))
            {
                // 씬 복제도 서로 다른 ID를 갖도록 새 소유자에게 새 번호를 발급한다.
                do
                {
                    id = -(int)((uint)Guid.NewGuid().GetHashCode() % int.MaxValue) - 1;
                } while (!reserved.Add(id));
                seen.Add(id);
            }

            if (identity.SceneBoxId == id && identity.SceneBoxEditorOwner == owner) continue;
            Undo.RecordObject(identity, "박스 고정 ID 자동 설정");
            identity.SetEditorSceneIdentity(id, owner);
            if (PrefabUtility.IsPartOfPrefabInstance(identity))
                PrefabUtility.RecordPrefabInstancePropertyModifications(identity);
            EditorUtility.SetDirty(identity);
            changed++;
        }

        if (changed > 0) EditorSceneManager.MarkSceneDirty(scene);
        return changed;
    }

    private static bool IsOriginal(ItemBox box)
    {
        var identity = box;
        return identity != null && identity.HasValidSceneBoxId &&
            identity.SceneBoxEditorOwner == GlobalObjectId.GetGlobalObjectIdSlow(box).ToString();
    }
}

public sealed class SceneItemBoxIdWindow : EditorWindow
{
    private string _result;

    [MenuItem("Tools/Item Boxes/씬 박스 ID 관리")]
    private static void Open() => GetWindow<SceneItemBoxIdWindow>("씬 박스 ID");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "씬의 ItemBox에 고정 ID를 자동 부여합니다. 비활성 박스도 포함하며 LootBox는 제외합니다.\n" +
            "씬 저장 시 누락·복제·중복 ID를 자동 보정합니다. 프리팹 원본에는 ID를 부여하지 않습니다.\n" +
            "게임 시작 시 ItemSpawner가 이 ID로 씬 박스를 등록하고 호스트의 내용물을 동기화합니다.", MessageType.Info);

        using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode ||
            PrefabStageUtility.GetCurrentPrefabStage() != null))
        {
            if (GUILayout.Button("열린 씬의 박스 ID 설정 (저장은 직접)"))
            {
                int count = 0;
                for (int i = 0; i < SceneManager.sceneCount; i++)
                    count += SceneItemBoxIdUtility.Repair(SceneManager.GetSceneAt(i));
                _result = $"{count}개 박스의 식별 정보를 설정했습니다. 씬을 저장해 주세요.";
            }

            if (GUILayout.Button("전체 맵 일괄 설정 및 저장 (Assets/Scenes)"))
                RepairAllMaps();
        }

        if (!string.IsNullOrEmpty(_result)) EditorGUILayout.HelpBox(_result, MessageType.Info);
    }

    private void RepairAllMaps()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
        {
            _result = "Assets/Scenes 폴더가 없습니다.";
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        int changed = 0;
        int processed = 0;
        var activeScene = SceneManager.GetActiveScene();
        try
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var scene = SceneManager.GetSceneByPath(path);
                bool openedHere = !scene.IsValid() || !scene.isLoaded;
                if (openedHere) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    changed += SceneItemBoxIdUtility.Repair(scene);
                    // 이미 열린 씬의 미저장 작업은 일괄 저장하지 않는다.
                    if (openedHere && scene.isDirty && !EditorSceneManager.SaveScene(scene))
                        throw new InvalidOperationException($"씬 저장에 실패했습니다: {path}");
                    processed++;
                }
                finally
                {
                    if (openedHere) EditorSceneManager.CloseScene(scene, true);
                }
            }
            _result = $"{processed}개 맵 처리, {changed}개 박스 설정 완료. 이미 열려 있던 씬은 직접 저장해 주세요.";
        }
        catch (Exception exception)
        {
            _result = $"처리 중 중단되었습니다: {exception.Message}";
            Debug.LogException(exception);
        }
        finally
        {
            if (activeScene.IsValid() && activeScene.isLoaded) SceneManager.SetActiveScene(activeScene);
        }
    }
}

[CustomEditor(typeof(ItemBox), true)]
[CanEditMultipleObjects]
public sealed class SceneItemBoxIdInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        var identity = (ItemBox)target;
        EditorGUILayout.LabelField("박스 고정 ID", identity.HasValidSceneBoxId ? identity.SceneBoxId.ToString() : "미발급");
        EditorGUILayout.HelpBox("씬 저장 시 자동으로 관리됩니다. ID를 직접 입력할 필요가 없습니다.", MessageType.Info);
    }
}
