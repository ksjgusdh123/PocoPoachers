using UnityEngine;
using UnityEngine.AI;

public class EnemyStat : StatBase
{
    [SerializeField] private int _enemyId;

    public int EnemyId => _enemyId;

    private TargetDetector _targetDetector;

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
