using System.Collections;
using UnityEngine;

public class DummyTarget : StatBase
{
    [SerializeField] private float _maxHp = 1000f;
    [SerializeField] private float _regenDelay = 5f;

    private Coroutine _regenCoroutine;

    protected override void Awake()
    {
        base.Awake();

        MaxHp = _maxHp;
        CurrentHp = MaxHp;
    }

    // base.TakeDamage는 HP가 0이 되면 반드시 Die()를 호출하므로, 총의 실제 데미지 수치는 그대로 표시하되
    // HP 저장값만 1로 바닥을 고정하기 위해 base를 거치지 않고 직접 처리한다
    public override bool TakeDamage(float damage, GameObject attacker = null)
    {
        if (IsDamageImmune) return false;

        float actualDamage = damage * (1f - Mathf.Clamp01(DefenseRate));
        CurrentHp = Mathf.Max(1f, CurrentHp - actualDamage);
        RaiseHpChanged();

        DamageTextUI.Show(actualDamage, transform.position);
        HpWorldUI.Show(this);
        ResetRegenTimer();

        return true;
    }

    private void ResetRegenTimer()
    {
        if (_regenCoroutine != null)
            StopCoroutine(_regenCoroutine);
        _regenCoroutine = StartCoroutine(RegenAfterDelay());
    }

    private IEnumerator RegenAfterDelay()
    {
        yield return new WaitForSeconds(_regenDelay);
        Heal(MaxHp);
    }
}
