using System;
using UnityEngine;

public abstract class StatBase : MonoBehaviour, IDamageable
{
    public float MaxHp { get; protected set; }
    public float CurrentHp { get; protected set; }

    public event Action<float, float> OnHpChanged;
    public event Action<float, Vector3> OnDamaged;
    public event Action OnDie;

    protected virtual void Awake()
    {
        OnDamaged += (damage, pos) => DamageTextPool.Show(damage, pos);
    }
    public void TakeDamage(float damage)
    {
        if (CurrentHp <= 0f) return;

        CurrentHp = Mathf.Max(0f, CurrentHp - damage);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
        OnDamaged?.Invoke(damage, transform.position);

        if (CurrentHp <= 0f)
            OnDie?.Invoke();
    }

    protected void RaiseHpChanged() => OnHpChanged?.Invoke(CurrentHp, MaxHp);
}
