using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 프리팹 3D를 임시로 렌더해 인벤/UI용 PNG 아이콘으로 저장합니다 (에디터 전용).
/// </summary>
public static class PrefabMakeIcon
{
    const int DefaultIconSize = 256;

    /// <summary>에셋 기준: Resources/Icons (테이블 icon 키 <c>Icons/...</c>와 동일).</summary>
    static string IconsOutputDirectory
    {
        get
        {
            var dir = Path.Combine(Application.dataPath, "Resources", "Icons");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    [MenuItem("Tools/PocoPoachers/프리팹 → 아이콘 PNG", false, 41)]
    public static void MakeIconFromToolsMenu()
    {
        var prefab = GetPrefabRootFromSelection();
        if (prefab == null)
        {
            EditorUtility.DisplayDialog("Make Icon", "Project 창에서 프리팹 루트(GameObject)를 선택하세요.", "OK");
            return;
        }

        var path = EditorUtility.SaveFilePanel("아이콘 PNG 저장", IconsOutputDirectory, prefab.name, "png");
        if (string.IsNullOrEmpty(path))
            return;

        try
        {
            RenderPrefabToIconPng(prefab, path, DefaultIconSize, DefaultIconSize);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Make Icon", "저장했습니다:\n" + path, "OK");
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex);
            EditorUtility.DisplayDialog("Make Icon", "실패: " + ex.Message, "OK");
        }
    }

    [MenuItem("Tools/PocoPoachers/프리팹 → 아이콘 PNG", true)]
    static bool MakeIconFromToolsMenuValidate() => GetPrefabRootFromSelection() != null;

    [MenuItem("Assets/PocoPoachers/프리팹 → 아이콘 PNG", false, 1210)]
    public static void MakeIconFromAssetsMenu()
    {
        foreach (var obj in Selection.objects)
        {
            if (obj is not GameObject go)
                continue;
            if (!IsPrefabAssetRoot(go))
                continue;

            var path = EditorUtility.SaveFilePanel("아이콘 PNG 저장", IconsOutputDirectory, go.name, "png");
            if (string.IsNullOrEmpty(path))
                return;

            RenderPrefabToIconPng(go, path, DefaultIconSize, DefaultIconSize);
            AssetDatabase.Refresh();
            EditorUtility.RevealInFinder(path);
            return;
        }

        EditorUtility.DisplayDialog("Make Icon", "Project 창에서 프리팹 루트를 하나 선택하세요.", "OK");
    }

    [MenuItem("Assets/PocoPoachers/프리팹 → 아이콘 PNG", true)]
    static bool MakeIconFromAssetsMenuValidate()
    {
        foreach (var obj in Selection.objects)
        {
            if (obj is GameObject go && IsPrefabAssetRoot(go))
                return true;
        }
        return false;
    }

    static GameObject GetPrefabRootFromSelection()
    {
        var obj = Selection.activeObject;
        return obj is GameObject go && IsPrefabAssetRoot(go) ? go : null;
    }

    static bool IsPrefabAssetRoot(GameObject go)
    {
        if (go == null || !EditorUtility.IsPersistent(go))
            return false;

        var t = PrefabUtility.GetPrefabAssetType(go);
        return t is PrefabAssetType.Regular or PrefabAssetType.Variant or PrefabAssetType.Model;
    }

    /// <summary>
    /// 프리팹 에셋 루트를 렌더해 PNG 아이콘으로 저장합니다.
    /// </summary>
    public static void RenderPrefabToIconPng(GameObject prefabAsset, string absoluteFilePath, int width, int height)
    {
        if (prefabAsset == null)
            throw new System.ArgumentNullException(nameof(prefabAsset));
        if (!IsPrefabAssetRoot(prefabAsset))
            throw new System.ArgumentException("prefabAsset은 Project 상의 프리팹 루트여야 합니다.", nameof(prefabAsset));

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
        instance.hideFlags = HideFlags.HideAndDontSave;
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        var camGo = EditorUtility.CreateGameObjectWithHideFlags("MakeIcon_Camera", HideFlags.HideAndDontSave);
        var keyLightGo = EditorUtility.CreateGameObjectWithHideFlags("MakeIcon_LightKey", HideFlags.HideAndDontSave);
        var fillLightGo = EditorUtility.CreateGameObjectWithHideFlags("MakeIcon_LightFill", HideFlags.HideAndDontSave);

        var cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        var captureAmbient = new Color(0.45f, 0.47f, 0.5f);
        cam.orthographic = false;
        cam.fieldOfView = 28f;
        cam.nearClipPlane = 0.01f;
        cam.farClipPlane = 5000f;
        cam.enabled = true;
        cam.allowHDR = false;
        cam.cullingMask = ~0;
        if (!cam.TryGetComponent<UniversalAdditionalCameraData>(out _))
            cam.gameObject.AddComponent<UniversalAdditionalCameraData>();

        void SetupDirLight(GameObject go, float intensity, Color color, Vector3 euler, LightShadows shadows)
        {
            var l = go.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = intensity;
            l.color = color;
            l.shadows = shadows;
            l.shadowStrength = 0.45f;
            go.transform.rotation = Quaternion.Euler(euler);
        }

        SetupDirLight(keyLightGo, 1.35f, new Color(1f, 0.98f, 0.95f), new Vector3(52f, -38f, 0f), LightShadows.Soft);
        SetupDirLight(fillLightGo, 0.65f, new Color(0.85f, 0.9f, 1f), new Vector3(18f, 125f, 0f), LightShadows.None);

        var prevAmbient = RenderSettings.ambientLight;
        var prevMode = RenderSettings.ambientMode;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = captureAmbient;

        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32)
        {
            antiAliasing = 4,
            hideFlags = HideFlags.HideAndDontSave
        };
        rt.Create();

        try
        {
            var bounds = CalculateWorldBounds(instance);
            var center = bounds.center;
            var radius = bounds.extents.magnitude;
            if (radius < 1e-4f)
                radius = 0.25f;

            var fovRad = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            var distance = Mathf.Max(0.05f, radius / Mathf.Max(0.001f, Mathf.Sin(fovRad)) * 1.12f);

            var dir = new Vector3(1f, 0.72f, 1f).normalized;
            cam.transform.position = center + dir * distance;
            cam.transform.LookAt(center, Vector3.up);

            cam.targetTexture = rt;
            cam.Render();

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;

            var bytes = tex.EncodeToPNG();
            Object.DestroyImmediate(tex);

            Directory.CreateDirectory(Path.GetDirectoryName(absoluteFilePath) ?? ".");
            File.WriteAllBytes(absoluteFilePath, bytes);
        }
        finally
        {
            RenderSettings.ambientLight = prevAmbient;
            RenderSettings.ambientMode = prevMode;

            cam.targetTexture = null;
            if (rt != null)
            {
                rt.Release();
                Object.DestroyImmediate(rt);
            }

            Object.DestroyImmediate(instance);
            Object.DestroyImmediate(fillLightGo);
            Object.DestroyImmediate(keyLightGo);
            Object.DestroyImmediate(camGo);
        }
    }

    static Bounds CalculateWorldBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return new Bounds(root.transform.position, Vector3.one * 0.35f);

        var b = renderers[0].bounds;
        for (var i = 1; i < renderers.Length; i++)
            b.Encapsulate(renderers[i].bounds);
        return b;
    }
}
