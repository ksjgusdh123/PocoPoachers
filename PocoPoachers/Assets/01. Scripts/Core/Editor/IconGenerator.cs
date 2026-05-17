using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public static class IconGenerator
{
    const int IconSize = 256;
    static string DefaultDir => Path.Combine(Application.dataPath, "_Art", "Resources", "Icons");

    [MenuItem("Tools/Generator/Prefab → Icon(PNG)", false, 41)]
    public static void CreateIconFromMenu()
    {
        // 현재 선택된 오브젝트 가져오기
        GameObject target = Selection.activeGameObject;

        if (!IsPrefabAsset(target))
        {
            EditorUtility.DisplayDialog("Make Icon", "Project 창에서 프리팹 에셋을 선택해주세요.", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanel("아이콘 저장", DefaultDir, target.name, "png");
        if (string.IsNullOrEmpty(path)) return;

        Render(target, path);
        AssetDatabase.Refresh();
        EditorUtility.RevealInFinder(path);
    }

    // Tools 메뉴 활성화 조건 (프리팹을 선택했을 때만 클릭 가능하게 하려면 추가, 아니면 삭제 가능)
    [MenuItem("Tools/Generator/Prefab → Icon(PNG)", true)]
    static bool ValidateCreateIcon() => IsPrefabAsset(Selection.activeGameObject);

    static bool IsPrefabAsset(GameObject go) =>
        go != null && EditorUtility.IsPersistent(go) && PrefabUtility.GetPrefabAssetType(go) != PrefabAssetType.NotAPrefab;

    private static void Render(GameObject prefab, string savePath)
    {
        var container = new GameObject("Temp_Icon_Root");
        var camGo = new GameObject("IconCam");
        var rt = RenderTexture.GetTemporary(IconSize, IconSize, 24, RenderTextureFormat.ARGB32);

        try
        {
            // 1. 프리팹 생성
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(container.transform);
            instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            // 2. 조명 세팅 (Key & Fill)
            CreateLight("KeyLight", new Vector3(50, -30, 0), 1.3f, container.transform);
            CreateLight("FillLight", new Vector3(20, 140, 0), 0.7f, container.transform);

            // 3. 카메라 세팅
            var cam = camGo.AddComponent<Camera>();
            cam.backgroundColor = new Color(0, 0, 0, 0);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.targetTexture = rt;
            if (!cam.TryGetComponent<UniversalAdditionalCameraData>(out _))
                cam.gameObject.AddComponent<UniversalAdditionalCameraData>();

            // 4. 구도 자동 잡기
            var bounds = CalculateBounds(instance);
            float dist = (bounds.extents.magnitude * 1.2f) / Mathf.Sin(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            cam.transform.position = bounds.center + new Vector3(1, 0.7f, 1).normalized * dist;
            cam.transform.LookAt(bounds.center);

            // 5. 렌더링 및 파일 저장
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, IconSize, IconSize), 0, 0);
            tex.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(savePath));
            File.WriteAllBytes(savePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
        }
        finally
        {
            RenderTexture.ReleaseTemporary(rt);
            Object.DestroyImmediate(container);
            Object.DestroyImmediate(camGo);
        }
    }

    static void CreateLight(string name, Vector3 rot, float intensity, Transform parent)
    {
        var l = new GameObject(name).AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = intensity;
        l.transform.rotation = Quaternion.Euler(rot);
        l.transform.SetParent(parent);
    }

    static Bounds CalculateBounds(GameObject obj)
    {
        var renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(obj.transform.position, Vector3.one);
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}