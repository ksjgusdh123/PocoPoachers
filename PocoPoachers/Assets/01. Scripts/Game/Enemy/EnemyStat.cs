using UnityEngine;
using UnityEngine.AI;

public class EnemyStat : StatBase
{
    [SerializeField] private int _enemyId;

    public int EnemyId => _enemyId;

    // 평생 1회만 허용되는 후퇴 여부
    public bool HasRetreated { get; private set; }

    public void MarkRetreated() => HasRetreated = true;

    private TargetDetector _targetDetector;
    private AIDodgeState _dodgeState;

    protected override void Awake()
    {
        base.Awake();

        var data = EnemyTable.Instance.Get(_enemyId);
        MaxHp = data?.MaxHp ?? 100f;
        CurrentHp = MaxHp;
        _totalDefenseRate = data?.DefenseRate ?? 0f;

        // 데이터테이블의 이동속도를 NavMeshAgent에 적용 (값이 없으면 프리팹 기본값 유지)
        if (data != null && data.MoveSpeed > 0f)
        {
            var agent = GetComponent<NavMeshAgent>();
            if (agent != null) agent.speed = data.MoveSpeed;
        }

        _targetDetector = GetComponent<TargetDetector>();
        _dodgeState = GetComponent<AIDodgeState>();
        OnDamaged += OnHit;
        OnDie += () => StartCoroutine(DeactivateNextFrame());
    }

    private void OnHit(float damage, Vector3 pos, GameObject attacker)
    {
        if (!RoomManager.IsHost || attacker == null) return;
        _targetDetector?.ForceSetTarget(attacker);
    }
    public override bool TakeDamage(float damage, GameObject attacker = null)
    {
        if (!RoomManager.IsHost) return false;

        // 회피(구르기)로 막을 수 있으면 데미지 없이 구르기 발동 — false 반환 시 총알은 관통 처리됨
        if (_dodgeState != null && _dodgeState.TryEvade(attacker))
        {
            if (attacker != null) _targetDetector?.ForceSetTarget(attacker);
            return false;
        }

        return base.TakeDamage(damage, attacker);
    }

    public void Initialize()
    {

    }

    private System.Collections.IEnumerator DeactivateNextFrame()
    {
        yield return null;
        HpWorldUI.Hide(this);
        gameObject.SetActive(false);
    }
}
