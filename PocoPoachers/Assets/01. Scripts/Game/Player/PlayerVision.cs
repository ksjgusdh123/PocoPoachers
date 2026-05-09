using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVision : MonoBehaviour
{
    [SerializeField] private float _detectRange = 15f;
    [SerializeField] private float _fovAngle = 90f;
    [SerializeField] private float _eyeHeight = 1.5f;
    [SerializeField] private float _detectInterval = 0.2f;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private LayerMask _wallLayer;

    public IReadOnlyList<GameObject> DetectedTargets => _detectedTargets;

    public event Action<GameObject> OnTargetDetected;
    public event Action<GameObject> OnTargetLost;

    private readonly List<GameObject> _detectedTargets = new();

    private void Start()
    {
        _wallLayer = 1 << LayerMask.NameToLayer("Wall");
        StartCoroutine(DetectionLoop());
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
        var colliders = Physics.OverlapSphere(transform.position, _detectRange, _targetLayer);

        var nextTargets = new List<GameObject>();
        foreach (var col in colliders)
        {
            if (!IsInFovAngle(col.transform)) continue;
            if (!HasLineOfSight(col.gameObject)) continue;
            nextTargets.Add(col.gameObject);
        }

        // 이번에 새로 감지된 타겟 이벤트
        foreach (var target in nextTargets)
        {
            if (!_detectedTargets.Contains(target))
                OnTargetDetected?.Invoke(target);
        }

        // 이번에 시야에서 벗어난 타겟 이벤트
        foreach (var target in _detectedTargets)
        {
            if (!nextTargets.Contains(target))
                OnTargetLost?.Invoke(target);
        }

        _detectedTargets.Clear();
        _detectedTargets.AddRange(nextTargets);
    }

    private bool IsInFovAngle(Transform target)
    {
        Vector3 dir = (target.position - transform.position).normalized;
        return Vector3.Angle(transform.forward, dir) <= _fovAngle * 0.5f;
    }

    private bool HasLineOfSight(GameObject target)
    {
        Vector3 origin = transform.position + Vector3.up * _eyeHeight;
        Collider col = target.GetComponent<Collider>();

        Vector3 center, top, bottom;
        if (col != null)
        {
            Bounds b = col.bounds;
            center = b.center;
            top = new Vector3(target.transform.position.x, b.max.y - 0.1f, target.transform.position.z);
            bottom = new Vector3(target.transform.position.x, b.min.y + 0.1f, target.transform.position.z);
        }
        else
        {
            center = target.transform.position + Vector3.up * _eyeHeight;
            top = center + Vector3.up * 0.5f;
            bottom = target.transform.position;
        }

        return IsRayClear(origin, center)
            || IsRayClear(origin, top)
            || IsRayClear(origin, bottom);
    }

    private bool IsRayClear(Vector3 origin, Vector3 point)
    {
        Vector3 dir = point - origin;
        bool blocked = Physics.Raycast(origin, dir.normalized, dir.magnitude, _wallLayer);
        Debug.DrawLine(origin, blocked ? origin + dir.normalized * dir.magnitude : point,
            blocked ? Color.red : Color.green, _detectInterval);
        return !blocked;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 eyePos = transform.position + Vector3.up * _eyeHeight;

        Gizmos.color = _detectedTargets.Count > 0 ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, _detectRange);

        Gizmos.color = Color.cyan;
        float half = _fovAngle * 0.5f;
        Gizmos.DrawRay(eyePos, Quaternion.Euler(0, -half, 0) * transform.forward * _detectRange);
        Gizmos.DrawRay(eyePos, Quaternion.Euler(0,  half, 0) * transform.forward * _detectRange);
    }
}
