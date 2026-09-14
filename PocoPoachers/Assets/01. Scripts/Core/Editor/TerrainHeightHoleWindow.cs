using UnityEditor;
using UnityEngine;

public sealed class TerrainHeightHoleWindow : EditorWindow
{
    [SerializeField] private Terrain _terrain;
    [SerializeField] private float _height = 3f;
    [SerializeField] private bool _worldHeight = true;
    private string _status;

    [MenuItem("Tools/Terrain/높이 기준 구멍 만들기")]
    public static void Open() => GetWindow<TerrainHeightHoleWindow>("높이 기준 구멍");

    private void OnEnable()
    {
        if (_terrain == null && Selection.activeGameObject != null)
            _terrain = Selection.activeGameObject.GetComponent<Terrain>();
        Undo.undoRedoPerformed += OnUndoRedo;
    }

    private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

    private void OnUndoRedo()
    {
        _status = "실행 취소/다시 실행됨. 영역 확인으로 현재 상태를 확인하세요.";
        if (_terrain != null) _terrain.Flush();
        SceneView.RepaintAll();
        Repaint();
    }

    private void OnGUI()
    {
        EditorGUI.BeginChangeCheck();
        _terrain = (Terrain)EditorGUILayout.ObjectField("대상 Terrain", _terrain, typeof(Terrain), true);
        _worldHeight = EditorGUILayout.Toggle("월드 Y 기준", _worldHeight);
        _height = EditorGUILayout.FloatField("이 높이 미만", _height);
        if (EditorGUI.EndChangeCheck()) _status = null;
        EditorGUILayout.HelpBox(
            "지정한 높이보다 낮은 칸에 구멍을 추가합니다. 경계 보호를 위해 칸의 네 꼭짓점이 모두 낮을 때만 적용합니다.\n"
            + "기존 구멍과 지형 높이는 유지됩니다. 내부 저지대도 포함됩니다.", MessageType.Info);
        EditorGUILayout.LabelField(_worldHeight ? "높이 기준: 씬의 월드 Y 좌표" : "높이 기준: Terrain 원점으로부터의 높이");
        bool valid = _terrain != null && _terrain.terrainData != null
            && !float.IsNaN(_height) && !float.IsInfinity(_height) && !EditorApplication.isPlaying;
        using (new EditorGUI.DisabledScope(!valid))
        {
            if (GUILayout.Button("영역 확인 (변경 없음)")) Process(false);
            if (GUILayout.Button("높이 기준으로 구멍 추가", GUILayout.Height(30))) Process(true);
            EditorGUILayout.Space();
            if (GUILayout.Button("구멍 가장자리 흙 마감 생성")) TerrainHoleRimBuilder.Build(_terrain, 3f);
        }
        if (!string.IsNullOrEmpty(_status)) EditorGUILayout.HelpBox(_status, MessageType.Info);
        EditorGUILayout.HelpBox("구멍 영역은 지면 충돌도 사라집니다. Ctrl+Z로 되돌릴 수 있습니다. 같은 TerrainData를 사용하는 Terrain에도 반영됩니다.", MessageType.None);
    }

    private void Process(bool apply)
    {
        var data = _terrain.terrainData;
        int resolution = data.holesResolution;
        if (resolution < 1 || data.heightmapResolution != resolution + 1)
        {
            _status = "이 Terrain의 높이맵과 구멍 해상도 조합은 지원하지 않습니다.";
            return;
        }
        try
        {
            var heights = data.GetHeights(0, 0, resolution + 1, resolution + 1);
            var holes = data.GetHoles(0, 0, resolution, resolution);
            float origin = _worldHeight ? _terrain.transform.position.y : 0f;
            float threshold = (_height - origin) / data.size.y;
            float minimum = float.MaxValue, maximum = float.MinValue;
            int added = 0;
            for (int z = 0; z <= resolution; z++)
            for (int x = 0; x <= resolution; x++)
            {
                minimum = Mathf.Min(minimum, heights[z, x]);
                maximum = Mathf.Max(maximum, heights[z, x]);
            }
            for (int z = 0; z < resolution; z++)
            {
                if (z % 64 == 0 && EditorUtility.DisplayCancelableProgressBar(
                    "높이 기준 구멍", "낮은 지형 영역을 확인하고 있습니다.", z / (float)resolution))
                {
                    _status = "취소했습니다. 터레인은 변경되지 않았습니다.";
                    return;
                }
                for (int x = 0; x < resolution; x++)
                {
                    // true는 지면, false는 구멍이다. 이미 뚫린 칸은 유지한다.
                    if (!holes[z, x] || heights[z, x] >= threshold
                        || heights[z + 1, x] >= threshold || heights[z, x + 1] >= threshold
                        || heights[z + 1, x + 1] >= threshold) continue;
                    holes[z, x] = false;
                    added++;
                }
            }
            if (apply && added > 0)
            {
                Undo.RegisterCompleteObjectUndo(data, "높이 기준 터레인 구멍 추가");
                data.SetHoles(0, 0, holes);
                EditorUtility.SetDirty(data);
                _terrain.Flush();
                SceneView.RepaintAll();
            }
            float area = added * (data.size.x / resolution) * (data.size.z / resolution);
            _status = $"{(apply ? "추가한" : "추가할")} 구멍: {added:N0}칸 ({area:N1}㎡)\n"
                + $"현재 기준의 지형 높이: {minimum * data.size.y + origin:F2}~{maximum * data.size.y + origin:F2}";
        }
        catch (System.Exception exception)
        {
            _status = "처리 중 오류가 발생했습니다. Console을 확인하세요.";
            Debug.LogException(exception);
        }
        finally { EditorUtility.ClearProgressBar(); }
    }
}
