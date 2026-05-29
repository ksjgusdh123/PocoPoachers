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

    public void TakeDamage(float damage, GameObject attacker = null)
    {
        if (CurrentHp <= 0f) return;

        CurrentHp = Mathf.Max(0f, CurrentHp - damage);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
        OnDamaged?.Invoke(damage, transform.position, attacker);
        OnLocalHpChanged(CurrentHp, MaxHp);

        if (CurrentHp <= 0f)
            OnDie?.Invoke();
    }

    // 로컬 HP 변화 시 추가 처리가 필요한 서브클래스에서 오버라이드
    protected virtual void OnLocalHpChanged(float hp, float maxHp) { }

    // 네트워크에서 받은 HP값을 직접 반영 (이벤트만 발생, 중복 sync 없음)
    public void SetHpFromNetwork(float hp, float maxHp)
    {
        MaxHp = maxHp;
        CurrentHp = hp;
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }

    protected void RaiseHpChanged() => OnHpChanged?.Invoke(CurrentHp, MaxHp);
}
