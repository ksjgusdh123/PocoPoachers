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

    // 매 프레임 GetComponentInParent를 호출하지 않도록 현재 타겟의 스탯을 캐싱한다.
    // 적이 많을 때 Update × 적 수만큼 계층 탐색이 발생하던 비용을 없앤다.
    private StatBase _currentTargetStat;

    // TryDetect가 매 호출 배열을 새로 만들지 않도록 재사용하는 버퍼.
    private Collider[] _overlapBuffer = new Collider[16];

    // 도발로 어그로가 고정된 동안의 시전자와 만료 시각. 이 사이에는 피격·소리로도 타겟이 넘어가지 않는다.
    private GameObject _tauntSource;
    private float _tauntExpireTime;

    // 현재 타겟 (없으면 null) — 스킬 등 외부에서 조회
    public GameObject CurrentTarget => _currentTarget;

    // 도발이 살아 있는지 — 시간이 지났거나 시전자가 죽었으면 풀린다.
    public bool IsTaunted =>
        _tauntSource != null && Time.time < _tauntExpireTime && IsAlive(_tauntSource);


    private void Awake()
    {
        _behaviorAgent = GetComponent<BehaviorGraphAgent>();
    }

    // 타겟이 죽으면(HP 0 이하) 즉시 무효화 — 행동 그래프에서 어떤 노드가 도는지와 무관하게 보장한다
    private void Update()
    {
        // 만료됐거나 시전자가 죽은 도발은 정리한다. 타겟 자체는 그대로 둬서
        // 도발이 풀린 뒤에도 일반 규칙(잊는 범위, 피격 반격)으로 이어지게 한다.
        if (_tauntSource != null && !IsTaunted)
            _tauntSource = null;

        if (_currentTarget == null) return;

        // 파괴됐거나(fake null 포함) 캐싱된 스탯의 HP가 0 이하면 해제
        if (_currentTargetStat != null && _currentTargetStat.CurrentHp <= 0f)
            ClearTarget();
    }

    public void SetDetectRange(float range) => _detectRange = range;

    // 피격 반격(EnemyStat)과 소리 감지(SoundDetector)가 쓰는 경로 — 도발 중에는 어그로를 넘기지 않는다.
    public void ForceSetTarget(GameObject target)
    {
        if (!IsAlive(target)) return;
        if (IsTaunted && target != _tauntSource) return;

        SetTarget(target);
    }

    // 도발 — duration 동안 시전자로 타겟을 고정한다. 이미 도발 중이면 더 늦게 끝나는 쪽으로 갱신한다.
    public void ApplyTaunt(GameObject source, float duration)
    {
        if (!IsAlive(source) || duration <= 0f) return;

        float expireTime = Time.time + duration;
        if (IsTaunted && expireTime < _tauntExpireTime && source == _tauntSource) return;

        _tauntSource = source;
        _tauntExpireTime = expireTime;
        SetTarget(source);
    }

    // 타겟 지정을 한 곳으로 모아 스탯 캐시와 블랙보드가 어긋나지 않게 한다.
    private void SetTarget(GameObject target)
    {
        _currentTarget = target;
        _currentTargetStat = target != null ? target.GetComponentInParent<StatBase>() : null;
        _behaviorAgent.BlackboardReference.SetVariableValue(_blackboardTarget, _currentTarget);
    }

    // 타겟으로 삼을 수 있는 대상인지 — 파괴됐거나 HP가 0 이하면 false
    // 스탯이 없는 대상(HP 개념이 없는 오브젝트)은 살아있는 것으로 취급한다
    // go가 GameObject 타입이라 == 이 유니티 오버로딩을 타므로 파괴된 오브젝트(fake null)도 걸러진다
    public static bool IsAlive(GameObject go)
    {
        if (go == null) return false;

        var stat = go.GetComponentInParent<StatBase>();
        return stat == null || stat.CurrentHp > 0f;
    }

    private void ClearTarget()
    {
        _tauntSource = null;
        _currentTarget = null;
        _currentTargetStat = null;
        _behaviorAgent.BlackboardReference.SetVariableValue(_blackboardTarget, (GameObject)null);
    }

    // 타겟이 없을 때: 탐지 거리 + 시야각 내 탐색
    public bool TryDetect()
    {
        if (_currentTarget != null)
            return true;

        // OverlapSphere는 매 호출 배열을 할당한다 — AI가 매 프레임 호출하므로 NonAlloc으로 대체.
        Collider[] buffer = _overlapBuffer;
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, _detectRange, buffer, _targetLayer);

        // 버퍼가 꽉 찼으면 후보가 잘렸을 수 있으므로 다음 호출을 위해 키운다.
        // 이번 결과는 방금 채워진 기존 버퍼(buffer)로 처리한다.
        if (count == buffer.Length)
            _overlapBuffer = new Collider[buffer.Length * 2];

        for (int i = 0; i < count; i++)
        {
            var col = buffer[i];
            if (col == null)
                continue;

            if (!IsInFov(col.transform))
                continue;

            if (!IsAlive(col.gameObject))
                continue;

            if (IsUndetectable(col.gameObject))
                continue;

            SetTarget(col.gameObject);
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
        if (dist > _forgetRange || !IsAlive(_currentTarget) || IsUndetectable(_currentTarget))
        {
            ClearTarget();
            return false;
        }
        return true;
    }

    // 은신 등으로 탐지에서 제외되는 대상인지 — 스탯이 없으면(HP 개념이 없는 대상) 항상 탐지 가능
    private static bool IsUndetectable(GameObject go)
    {
        return go.TryGetComponent<StatBase>(out var stat) && stat.IsUndetectable;
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
