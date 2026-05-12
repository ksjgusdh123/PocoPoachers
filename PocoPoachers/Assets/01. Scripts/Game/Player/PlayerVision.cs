using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVision : MonoBehaviour
{
    [SerializeField] private VisionConfig _visionConfig;
    [SerializeField] private float _eyeHeight = 1.5f;
    [SerializeField] private float _detectInterval = 0.2f;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private LayerMask _wallLayer;

    [Header("시야 시각화")]
    [SerializeField] private bool _showVision = true;
    [SerializeField] private Color _visionColor = new Color(0f, 1f, 1f, 0.8f);
    [SerializeField] private float _lineWidth = 0.05f;

    public IReadOnlyList<GameObject> DetectedTargets => _detectedTargets;

    public event Action<GameObject> OnTargetDetected;
    public event Action<GameObject> OnTargetLost;

    private readonly List<GameObject> _detectedTargets = new();
    private LineRenderer _lineRenderer;

    private void Awake()
    {
        if (_showVision)
            InitLineRenderer();
    }

    private void Start()
    {
        _wallLayer = 1 << LayerMask.NameToLayer("Wall");
        StartCoroutine(DetectionLoop());
    }

    private void Update()
    {
        if (_showVision)
            UpdateVisionLine();
    }

    private void InitLineRenderer()
    {
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.useWorldSpace = true;
        _lineRenderer.loop = false;
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.startColor = _visionColor;
        _lineRenderer.endColor = _visionColor;

        // 중앙(1) + 호(arcSegments + 1) + 중앙(1) = arcSegments + 3
        _lineRenderer.positionCount = _visionConfig.arcSegments + 3;
    }

    // 매 프레임 오브젝트 회전/이동에 맞춰 선 갱신
    private void UpdateVisionLine()
    {
        Vector3 eyePos = transform.position + Vector3.up * _eyeHeight;
        float half = _visionConfig.fovAngle * 0.5f;
        int segments = _visionConfig.arcSegments;

        // 0번: 중앙(시작)
        _lineRenderer.SetPosition(0, eyePos);

        // 1 ~ arcSegments+1번: 각 방향마다 Raycast → 벽에 막히면 hit.point에서 끊김
        for (int i = 0; i <= segments; i++)
        {
            float angle = -half + _visionConfig.fovAngle / segments * i;
            Vector3 dir = Quaternion.Euler(0, angle, 0) * transform.forward;

            Vector3 endpoint = Physics.Raycast(eyePos, dir, out RaycastHit hit, _visionConfig.detectRange, _wallLayer)
                ? hit.point
                : eyePos + dir * _visionConfig.detectRange;

            _lineRenderer.SetPosition(i + 1, endpoint);
        }

        // 마지막: 중앙(끝)
        _lineRenderer.SetPosition(segments + 2, eyePos);
    }

    private IEnumerator DetectionLoop()
    {
        var wait = new WaitForSeconds(_detectInterval);
        while (true)
        {
            Scan();
            yield return wait;
        }
    }

    private void Scan()
    {
        var colliders = Physics.OverlapSphere(transform.position, _visionConfig.detectRange, _targetLayer);

        var nextTargets = new List<GameObject>();
        foreach (var col in colliders)
        {
            if (!IsInFovAngle(col.transform)) continue;
            if (!HasLineOfSight(col.gameObject)) continue;
            nextTargets.Add(col.gameObject);
        }

        // 이번에 새로 감지된 타겟: 렌더러 활성화
        foreach (var target in nextTargets)
        {
            if (!_detectedTargets.Contains(target))
            {
                SetRenderersEnabled(target, true);
                OnTargetDetected?.Invoke(target);
            }
        }

        // 이번에 시야에서 벗어난 타겟: 렌더러 비활성화
        foreach (var target in _detectedTargets)
        {
            if (!nextTargets.Contains(target))
            {
                SetRenderersEnabled(target, false);
                OnTargetLost?.Invoke(target);
            }
        }

        _detectedTargets.Clear();
        _detectedTargets.AddRange(nextTargets);
    }

    private void SetRenderersEnabled(GameObject target, bool enabled)
    {
        VisionTarget visionTarget = target.GetComponentInParent<VisionTarget>();
        if (visionTarget != null)
        {
            visionTarget.SetVisible(enabled);
            return;
        }

        foreach (var r in target.GetComponentsInChildren<Renderer>())
            r.enabled = enabled;
    }

    private bool IsInFovAngle(Transform target)
    {
        Vector3 dir = (target.position - transform.position).normalized;
        return Vector3.Angle(transform.forward, dir) <= _visionConfig.fovAngle * 0.5f;
    }

    private bool HasLineOfSight(GameObject target)
    {
        Vector3 origin = transform.position + Vector3.up * _eyeHeight;
        Vector3 point = target.transform.position;
        point.y = origin.y;

        return IsRayClear(origin, point); 
    }

    private bool IsRayClear(Vector3 origin, Vector3 point)
    {
        point.y = origin.y;

        Vector3 dir = point - origin;
        bool blocked = Physics.Raycast(origin, dir.normalized, dir.magnitude, _wallLayer);

        Debug.DrawLine(
            origin,
            blocked ? origin + dir.normalized * dir.magnitude : point,
            blocked ? Color.red : Color.green,
            _detectInterval
        );

        return !blocked;
    }

    private void OnDrawGizmosSelected()
    {
        if (_visionConfig == null) return;

        Vector3 eyePos = transform.position + Vector3.up * _eyeHeight;
        float range = _visionConfig.detectRange;
        float half = _visionConfig.fovAngle * 0.5f;

        Gizmos.color = _detectedTargets.Count > 0 ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, range);

        Gizmos.color = Color.cyan;
        Vector3 leftDir  = Quaternion.Euler(0, -half, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0,  half, 0) * transform.forward;
        Gizmos.DrawRay(eyePos, leftDir  * range);
        Gizmos.DrawRay(eyePos, rightDir * range);

        Vector3 prev = eyePos + leftDir * range;
        for (int i = 1; i <= 20; i++)
        {
            float angle = -half + _visionConfig.fovAngle / 20 * i;
            Vector3 next = eyePos + Quaternion.Euler(0, angle, 0) * transform.forward * range;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
