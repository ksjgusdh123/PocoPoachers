using UnityEngine;

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

        _targetDetector = GetComponent<TargetDetector>();
        OnDamaged += OnHit;
        OnDie += () => StartCoroutine(DeactivateNextFrame());
    }

    private void OnHit(float damage, Vector3 pos, GameObject attacker)
    {
        if (!RoomManager.IsHost || attacker == null) return;
        _targetDetector?.ForceSetTarget(attacker);
    }
    public override void TakeDamage(float damage, GameObject attacker = null)
    {
        if (!RoomManager.IsHost) return;
        base.TakeDamage(damage, attacker);
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
