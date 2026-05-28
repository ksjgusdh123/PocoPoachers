using System;
using UnityEngine;

public abstract class StatBase : MonoBehaviour, IDamageable
{
    public float MaxHp { get; protected set; }
    public float CurrentHp { get; protected set; }

    public event Action<float, float> OnHpChanged;
    public event Action<float, Vector3, GameObject> OnDamaged;
    public event Action OnDie;

    protected virtual void Awake()
    {
        OnDamaged += (damage, pos, _) => DamageTextUI.Show(damage, pos);
        OnDamaged += (_, __, ___) => HpWorldUI.Show(this);
    }

    protected virtual float Defense => 0f;

    public void TakeDamage(float damage, GameObject attacker = null)
    {
        if (CurrentHp <= 0f) return;

        float actualDamage = Mathf.Max(0f, damage - Defense);
        CurrentHp = Mathf.Max(0f, CurrentHp - actualDamage);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
        OnDamaged?.Invoke(actualDamage, transform.position, attacker);

        if (CurrentHp <= 0f)
            OnDie?.Invoke();
    }

    protected void RaiseHpChanged() => OnHpChanged?.Invoke(CurrentHp, MaxHp);
}
