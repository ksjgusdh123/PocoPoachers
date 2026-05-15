using Unity.Behavior;
using UnityEngine;

public class TargetDetector : MonoBehaviour
{
    [SerializeField] private float _detectRange = 10f;
    [SerializeField] private float _forgetRange = 100f;
    [SerializeField] private float _fovAngle = 120f;
    [SerializeField] private LayerMask _targetLayer;
    [SerializeField] private string _blackboardTarget = "Target";

    private BehaviorGraphAgent _behaviorAgent;
    private GameObject _currentTarget;


    private void Awake()
    {
        _behaviorAgent = GetComponent<BehaviorGraphAgent>();
    }

    private void Update()
    {
    }

    public void SetDetectRange(float range) => _detectRange = range;

    public void ForceSetTarget(GameObject target)
    {
        _currentTarget = target;
        _behaviorAgent.BlackboardReference.SetVariableValue(_blackboardTarget, _currentTarget);
    }

    // 타겟이 없을 때: 탐지 거리 + 시야각 내 탐색
    public bool TryDetect()
    {
        if (_currentTarget != null)
            return true;

        var colliders = Physics.OverlapSphere(transform.position, _detectRange, _targetLayer);
        foreach (var col in colliders)
        {
            if (!IsInFov(col.transform))
                continue;

            _currentTarget = col.gameObject;
            _behaviorAgent.BlackboardReference.SetVariableValue(_blackboardTarget, _currentTarget);
            return true;
        }
        return false;
    }

    private bool IsInFov(Transform target)
    {
        Vector3 dir = (target.position - transform.position).normalized;    
        return Vector3.Angle(transform.forward, dir) <= _fovAngle * 0.5f;
    }

    // 타겟이 인지 범위 안에 있는지 확인
    public bool IsTargetInDetectRange()
    {
        if (_currentTarget == null) return false;
        return Vector3.Distance(transform.position, _currentTarget.transform.position) <= _detectRange;
    }

    // 타겟이 있을 때: 잊는 거리 벗어나면 해제
    public bool IsTargetInForgetRange()
    {
        if (_currentTarget == null)
            return false;

        float dist = Vector3.Distance(transform.position, _currentTarget.transform.position);
        if (dist > _forgetRange)
        {
            _behaviorAgent.BlackboardReference.SetVariableValue(_blackboardTarget, (GameObject)null);
            _currentTarget = null;
            return false;
        }
        return true;
    }

    private void OnDrawGizmosSelected()
    {
        // 탐지 범위: 초록/빨강
        Gizmos.color = _currentTarget != null ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, _detectRange);

        // 잊는 범위: 노란색
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _forgetRange);

        // 시야각: 탐지 범위 기준 부채꼴
        Gizmos.color = Color.cyan;
        float half = _fovAngle * 0.5f;
        Vector3 leftDir = Quaternion.Euler(0, -half, 0) * transform.forward;
        Vector3 rightDir = Quaternion.Euler(0, half, 0) * transform.forward;
        Gizmos.DrawRay(transform.position, leftDir * _detectRange);
        Gizmos.DrawRay(transform.position, rightDir * _detectRange);
    }
}
