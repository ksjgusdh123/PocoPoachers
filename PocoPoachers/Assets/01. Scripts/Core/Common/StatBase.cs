using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public abstract class StatBase : MonoBehaviour, IDamageable
{
    public float MaxHp { get; protected set; }
    public float CurrentHp { get; protected set; }

    public event Action<float, float> OnHpChanged;
    public event Action<float, Vector3, GameObject> OnDamaged;
    public event Action OnDie;

    public bool IsDead { get; private set; }

    protected float _totalDefenseRate;

    protected virtual void Awake()
    {
        OnDamaged += (damage, pos, _) => DamageTextUI.Show(damage, pos);
        OnDamaged += (_, __, ___) => HpWorldUI.Show(this);
    }

    protected virtual float DefenseRate => _totalDefenseRate;

    public virtual void ApplyArmorStat(ArmorStatData data)
    {
        _totalDefenseRate += data.DefenseRate;
    }

    public virtual void RemoveArmorStat(ArmorStatData data)
    {
        _totalDefenseRate = Mathf.Max(0f, _totalDefenseRate - data.DefenseRate);
    }

    public virtual bool TakeDamage(float damage, GameObject attacker = null)
    {
        if (CurrentHp <= 0f) return false;

        float actualDamage = damage * (1f - Mathf.Clamp01(DefenseRate));
        CurrentHp = Mathf.Max(0f, CurrentHp - actualDamage);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
        OnDamaged?.Invoke(actualDamage, transform.position, attacker);
        OnLocalHpChanged(CurrentHp, MaxHp);

        if (CurrentHp <= 0f)
            Die();

        return true;
    }

    // 사망 처리 — HP 고갈, 배터리 방전 등에서 호출. 한 번만 OnDie 발생
    protected void Die()
    {
        if (IsDead) return;
        IsDead = true;
        OnDie?.Invoke();
    }

    // 로컬 HP 변화 시 추가 처리가 필요한 서브클래스에서 오버라이드
    protected virtual void OnLocalHpChanged(float hp, float maxHp) { }

    // 네트워크에서 받은 HP값을 직접 반영 (이벤트만 발생, 중복 sync 없음)
    public void SetHpFromNetwork(float hp, float maxHp, float damage)
    {
        MaxHp = maxHp;
        CurrentHp = hp;
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
        if (damage > 0f) OnDamaged?.Invoke(damage, transform.position, null);
    }

    protected void RaiseHpChanged() => OnHpChanged?.Invoke(CurrentHp, MaxHp);
}
