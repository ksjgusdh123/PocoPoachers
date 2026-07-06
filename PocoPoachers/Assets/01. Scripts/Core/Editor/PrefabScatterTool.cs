using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 배경 장식 프리팹을 씬 뷰에서 브러시로 흩뿌려 배치하는 툴 (Polybrush 대체용 간이 버전)
public class PrefabScatterTool : EditorWindow
{
    [SerializeField] private List<GameObject> _prefabs = new List<GameObject>();
    [SerializeField] private Transform _parent;
    [SerializeField] private LayerMask _surfaceMask = ~0;
    [SerializeField] private float _radius = 3f;
    [SerializeField] private int _countPerClick = 5;
    [SerializeField] private float _scaleMin = 0.8f;
    [SerializeField] private float _scaleMax = 1.2f;
    [SerializeField] private bool _randomYRotation = true;
    [SerializeField] private bool _alignToNormal;

    private bool _painting;
    private Vector3 _lastPaintPos;
    private SerializedObject _so;
    private SerializedProperty _prefabsProp;

    [MenuItem("Tools/Level Design/Prefab Scatter")]
    private static void Open() => GetWindow<PrefabScatterTool>("Prefab Scatter");

    private void OnEnable()
    {
        _so = new SerializedObject(this);
        _prefabsProp = _so.FindProperty("_prefabs");
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        _so.Update();

        EditorGUILayout.PropertyField(_prefabsProp, new GUIContent("뿌릴 프리팹 목록"), true);
        _so.ApplyModifiedProperties();

        _parent = (Transform)EditorGUILayout.ObjectField("생성될 부모", _parent, typeof(Transform), true);
        _surfaceMask = LayerMaskField("적용 대상 레이어", _surfaceMask);
        _radius = EditorGUILayout.Slider("브러시 반경", _radius, 0.5f, 30f);
        _countPerClick = EditorGUILayout.IntSlider("1회 클릭당 개수", _countPerClick, 1, 50);
        EditorGUILayout.MinMaxSlider("스케일 범위", ref _scaleMin, ref _scaleMax, 0.1f, 3f);
        EditorGUILayout.LabelField($"{_scaleMin:F2} ~ {_scaleMax:F2}");
        _randomYRotation = EditorGUILayout.Toggle("Y축 랜덤 회전", _randomYRotation);
        _alignToNormal = EditorGUILayout.Toggle("표면 노멀에 정렬", _alignToNormal);

        EditorGUILayout.Space();
        GUI.backgroundColor = _painting ? Color.green : Color.white;
        if (GUILayout.Button(_painting ? "페인팅 중 (씬 뷰 좌클릭/드래그로 배치, Alt=회전)" : "페인팅 시작", GUILayout.Height(30)))
            _painting = !_painting;
        GUI.backgroundColor = Color.white;

        if (_prefabs.Count == 0)
            EditorGUILayout.HelpBox("뿌릴 프리팹을 최소 1개 이상 등록하세요.", MessageType.Warning);
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        sceneView.wantsMouseMove = true;

        if (!_painting || _prefabs.Count == 0) return;

        Event e = Event.current;
        if (e.alt) return; // Alt를 누른 상태(씬 뷰 회전 조작)에서는 페인팅하지 않음

        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
        bool hasHit = Physics.Raycast(ray, out RaycastHit hit, 1000f, _surfaceMask);

        if (hasHit)
        {
            Handles.color = new Color(0f, 1f, 0.4f, 0.6f);
            Handles.DrawWireDisc(hit.point, hit.normal, _radius);
        }

        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
        {
            if (hasHit && (e.type == EventType.MouseDown || Vector3.Distance(hit.point, _lastPaintPos) > _radius * 0.5f))
            {
                Scatter(hit.point, hit.normal);
                _lastPaintPos = hit.point;
            }
            e.Use();
        }
        else if (e.type == EventType.MouseUp && e.button == 0)
        {
            e.Use();
        }

        // 페인팅 모드에서는 씬 뷰 기본 좌클릭(오브젝트 선택)을 막기 위해 컨트롤을 점유
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        sceneView.Repaint();
    }

    private void Scatter(Vector3 center, Vector3 normal)
    {
        // 프로젝트(에셋)의 프리팹 Transform이 부모로 잘못 지정된 경우 방지 (씬 오브젝트만 허용)
        Transform parent = (_parent != null && !EditorUtility.IsPersistent(_parent)) ? _parent : null;
        if (_parent != null && parent == null)
            Debug.LogWarning("Prefab Scatter: '생성될 부모'에는 씬에 있는 오브젝트만 지정할 수 있습니다. 부모 없이 배치합니다.");

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        for (int i = 0; i < _countPerClick; i++)
        {
            Vector2 offset2D = Random.insideUnitCircle * _radius;
            Vector3 samplePos = center + new Vector3(offset2D.x, 50f, offset2D.y);

            if (!Physics.Raycast(samplePos, Vector3.down, out RaycastHit hit, 200f, _surfaceMask))
                continue;

            GameObject prefab = _prefabs[Random.Range(0, _prefabs.Count)];
            if (prefab == null) continue;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.transform.position = hit.point;

            Quaternion rotation = _alignToNormal ? Quaternion.FromToRotation(Vector3.up, hit.normal) : Quaternion.identity;
            if (_randomYRotation)
                rotation *= Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            instance.transform.rotation = rotation;

            instance.transform.localScale = Vector3.one * Random.Range(_scaleMin, _scaleMax);

            Undo.RegisterCreatedObjectUndo(instance, "Scatter Prefab");
        }

        Undo.CollapseUndoOperations(undoGroup);
    }

    // 다중 선택 가능한 LayerMask 필드 (Unity 표준 우회 패턴)
    private static LayerMask LayerMaskField(string label, LayerMask layerMask)
    {
        var layerNames = new List<string>();
        var layerNumbers = new List<int>();

        for (int i = 0; i < 32; i++)
        {
            string layerName = LayerMask.LayerToName(i);
            if (string.IsNullOrEmpty(layerName)) continue;
            layerNames.Add(layerName);
            layerNumbers.Add(i);
        }

        int shownMask = 0;
        for (int i = 0; i < layerNumbers.Count; i++)
        {
            if ((layerMask.value & (1 << layerNumbers[i])) != 0)
                shownMask |= 1 << i;
        }

        shownMask = EditorGUILayout.MaskField(label, shownMask, layerNames.ToArray());

        int result = 0;
        for (int i = 0; i < layerNumbers.Count; i++)
        {
            if ((shownMask & (1 << i)) != 0)
                result |= 1 << layerNumbers[i];
        }

        layerMask.value = result;
        return layerMask;
    }
}
