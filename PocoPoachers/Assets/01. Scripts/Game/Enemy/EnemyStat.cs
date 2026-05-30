using UnityEngine;

public class EnemyStat : StatBase
{
    [SerializeField] private int _monsterId;

    private TargetDetector _targetDetector;

    protected override void Awake()
    {
        base.Awake();
        MaxHp = 100f;
        CurrentHp = 100f;
        _targetDetector = GetComponent<TargetDetector>();
        OnDamaged += OnHit;
        OnDie += () => StartCoroutine(DeactivateNextFrame());   
    }

    private void OnHit(float damage, Vector3 pos, GameObject attacker)
    {
        if (attacker == null) return;
        _targetDetector?.ForceSetTarget(attacker);
    }

    public void Initialize()
    {

    }

    private System.Collections.IEnumerator DeactivateNextFrame()
    {
        yield return null;
        gameObject.SetActive(false);
    }
}
