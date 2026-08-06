using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

// 현재 씬의 Terrain을 위에서 내려다본 정적 스크린샷을 찍어 PNG로 저장하는 에디터 툴.
// Culling Mask로 원하는 레이어만 골라 찍을 수 있다 (건물/오브젝트 레이어를 빼면 지형만 찍힘).
public class MinimapCaptureWindow : EditorWindow
{
    const string GeneratedAssetDir = "Assets/_Generated/Minimap";

    Terrain targetTerrain;
    int resolution = 1024;
    float heightMargin = 50f; // 지형 최고 높이 위로 이만큼 띄워서 촬영
    LayerMask cullingMask = ~0; // 기본은 전체 레이어

    bool useCustomArea;
    Transform areaMarker; // 씬에 빈 오브젝트를 두고 여기 연결하면 그 위치가 중심이 됨 (드래그로 조정 가능)
    Vector3 customCenter = Vector3.zero; // 마커 없을 때만 사용. Y는 무시하고 X/Z만 씀
    float customSize = 100f; // 정사각형 촬영 영역의 한 변 길이

    Vector3 EffectiveCenter => areaMarker != null ? areaMarker.position : customCenter;

    [MenuItem("Tools/Generator/Minimap Capture")]
    public static void Open() => GetWindow<MinimapCaptureWindow>("Minimap Capture");

    void OnEnable() => SceneView.duringSceneGui += OnSceneGUI;
    void OnDisable() => SceneView.duringSceneGui -= OnSceneGUI;

    void OnGUI()
    {
        EditorGUILayout.LabelField("촬영 설정", EditorStyles.boldLabel);
        targetTerrain = (Terrain)EditorGUILayout.ObjectField("Terrain (비우면 활성 Terrain 사용)", targetTerrain, typeof(Terrain), true);
        resolution = EditorGUILayout.IntField("해상도 (px)", resolution);
        heightMargin = EditorGUILayout.FloatField("촬영 높이 여유", heightMargin);
        cullingMask = LayerMaskField("찍힐 레이어 (Culling Mask)", cullingMask);

        EditorGUILayout.Space(8);
        useCustomArea = EditorGUILayout.Toggle("영역 직접 지정", useCustomArea);
        using (new EditorGUI.DisabledScope(!useCustomArea))
        {
            areaMarker = (Transform)EditorGUILayout.ObjectField("영역 마커 (씬에 빈 오브젝트 연결)", areaMarker, typeof(Transform), true);
            using (new EditorGUI.DisabledScope(areaMarker != null))
                customCenter = EditorGUILayout.Vector3Field("중심 좌표 (마커 없을 때만)", customCenter);
            customSize = EditorGUILayout.FloatField("촬영 폭 (한 변, m)", customSize);
            EditorGUILayout.HelpBox("씬 뷰에 노란 박스로 촬영 범위가 표시됩니다. 마커를 씬에서 직접 드래그해 위치를 맞추세요.", MessageType.Info);
        }

        EditorGUILayout.Space(12);
        if (GUILayout.Button("스크린샷 찍기", GUILayout.Height(32)))
            Capture();
    }

    // 유니티 EditorGUILayout엔 다중 레이어 마스크 필드가 기본 제공되지 않아 InternalEditorUtility로 변환해 구현한다
    static LayerMask LayerMaskField(string label, LayerMask layerMask)
    {
        string[] layers = InternalEditorUtility.layers;
        int mask = InternalEditorUtility.LayerMaskToConcatenatedLayersMask(layerMask);
        mask = EditorGUILayout.MaskField(label, mask, layers);
        return InternalEditorUtility.ConcatenatedLayersMaskToLayerMask(mask);
    }

    void OnSceneGUI(SceneView view)
    {
        if (!useCustomArea) return;

        Terrain terrain = targetTerrain != null ? targetTerrain : Terrain.activeTerrain;
        float y0 = terrain != null ? terrain.transform.position.y : 0f;
        float y1 = terrain != null ? y0 + terrain.terrainData.size.y : y0 + 50f;

        Vector3 center = EffectiveCenter;
        Vector3 boxCenter = new Vector3(center.x, (y0 + y1) * 0.5f, center.z);
        Vector3 boxSize = new Vector3(customSize, y1 - y0, customSize);

        Handles.color = Color.yellow;
        Handles.DrawWireCube(boxCenter, boxSize);
    }

    void Capture()
    {
        Terrain terrain = targetTerrain != null ? targetTerrain : Terrain.activeTerrain;
        if (terrain == null)
        {
            Debug.LogError("[MinimapCapture] 대상 Terrain이 없습니다.");
            return;
        }

        Vector3 size = terrain.terrainData.size;
        Vector3 origin = terrain.transform.position;
        float captureY = origin.y + size.y + heightMargin;

        // 영역 직접 지정 시 중심/폭을 지형 전체 대신 직접 입력한 값으로 촬영한다
        Vector3 center = useCustomArea
            ? new Vector3(EffectiveCenter.x, 0f, EffectiveCenter.z)
            : origin + new Vector3(size.x * 0.5f, 0f, size.z * 0.5f);
        float orthoSize = useCustomArea ? customSize * 0.5f : Mathf.Max(size.x, size.z) * 0.5f;

        var camGo = new GameObject("MinimapCaptureCamera_TEMP");
        camGo.transform.position = new Vector3(center.x, captureY, center.z);
        camGo.transform.rotation = Quaternion.Euler(90f, 0f, 0f); // 정면 아래를 보도록

        Camera cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = orthoSize;
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = size.y + heightMargin + 50f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.cullingMask = cullingMask;

        var rt = new RenderTexture(resolution, resolution, 24);
        cam.targetTexture = rt;
        cam.Render();

        RenderTexture.active = rt;
        var tex = new Texture2D(resolution, resolution, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, resolution, resolution), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        cam.targetTexture = null;
        DestroyImmediate(camGo);
        rt.Release();
        DestroyImmediate(rt);

        if (!Directory.Exists(GeneratedAssetDir))
            Directory.CreateDirectory(GeneratedAssetDir);

        string sceneName = EditorSceneManager.GetActiveScene().name;
        string path = $"{GeneratedAssetDir}/{sceneName}_Minimap.png";
        File.WriteAllBytes(path, tex.EncodeToPNG());
        DestroyImmediate(tex);

        AssetDatabase.Refresh();

        // 런타임에서 월드 좌표 → 미니맵 좌표 변환에 쓸 촬영 범위 정보를 텍스처와 함께 저장한다
        string dataPath = $"{GeneratedAssetDir}/{sceneName}_Minimap.asset";
        var data = AssetDatabase.LoadAssetAtPath<MinimapCaptureData>(dataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MinimapCaptureData>();
            AssetDatabase.CreateAsset(data, dataPath);
        }
        data.MinimapTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        data.WorldCenter = center;
        data.WorldSize = orthoSize * 2f;
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        Debug.Log($"[MinimapCapture] 저장됨: {path}, {dataPath}");
        Selection.activeObject = data;
    }
}
