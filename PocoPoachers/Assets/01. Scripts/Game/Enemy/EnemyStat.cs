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

        // 적의 공격력/발사 간격 배율. 공격력은 Bullet이 공격자 스탯에서 읽어 곱하고,
        // 발사 간격은 GunBase가 총의 rpm에 반영한다 — 둘 다 플레이어와 같은 경로다.
        // 0 이하가 들어오면 데미지가 사라지거나 0으로 나누게 되므로 기본값으로 되돌린다.
        AttackPowerMultiplier = data != null && data.AttackPowerMultiplier > 0f
            ? data.AttackPowerMultiplier : DefaultAttackPowerMultiplier;
        FireDelayMultiplier = data != null && data.FireDelayMultiplier > 0f
            ? data.FireDelayMultiplier : DefaultFireDelayMultiplier;

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
