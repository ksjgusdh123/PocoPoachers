using System;
using UnityEngine;

public abstract class StatBase : MonoBehaviour, IDamageable
{
    public float MaxHp { get; protected set; }
    public float CurrentHp { get; protected set; }

    public event Action<float, float> OnHpChanged;
    public event Action<float, Vector3, GameObject> OnDamaged;
    public event Action OnDie;
    public event Action OnRevive;

    public bool IsDead { get; private set; }

    // 무적 상태 (구르기 등에서 켜고 끔) — 켜져 있으면 데미지를 받지 않음
    public bool IsInvincible { get; private set; }
    public void SetInvincible(bool value) => IsInvincible = value;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 치트 무적 — 구르기 무적과 별개로 유지되어 구르기 종료에 꺼지지 않음
    public bool IsGodMode { get; private set; }
    public void SetGodMode(bool value) => IsGodMode = value;
#endif

    protected float _totalDefenseRate;
    protected float _enhancementDefenseRateBonus;

    protected virtual void Awake()
    {
        OnDamaged += (damage, pos, _) => DamageTextUI.Show(damage, pos);
        OnDamaged += (_, __, ___) => HpWorldUI.Show(this);
    }

    protected virtual float DefenseRate => _totalDefenseRate + _enhancementDefenseRateBonus;

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
        if (IsInvincible) return false;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (IsGodMode) return false;
#endif
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
    // HP를 0으로 확정해야 배터리 방전 같은 비전투 사망도 네트워크(StatSync)로 전파된다
    protected void Die()
    {
        if (IsDead) return;
        IsDead = true;

        if (CurrentHp > 0f)
        {
            CurrentHp = 0f;
            RaiseHpChanged();
            OnLocalHpChanged(CurrentHp, MaxHp);
        }

        OnDie?.Invoke();
    }

    // 사망 상태에서 되살리기 — Heal은 HP 0에서 막히고 IsDead도 되돌리지 못하므로 부활은 이 경로로만 가능하다
    // IsDead를 풀지 않으면 Die()가 다시 발동하지 않아 두 번째 죽음이 무시된다
    public void Revive(float hp)
    {
        if (!IsDead && CurrentHp > 0f) return;

        IsDead = false;
        CurrentHp = Mathf.Clamp(hp, 1f, MaxHp);
        RaiseHpChanged();
        OnRevive?.Invoke();
        OnLocalHpChanged(CurrentHp, MaxHp);
    }

    public void Heal(float amount)
    {
        if (CurrentHp <= 0f) return;

        CurrentHp = Mathf.Min(MaxHp, CurrentHp + amount);
        RaiseHpChanged();
        OnLocalHpChanged(CurrentHp, MaxHp);
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

        // 호스트 권한으로 판정된 죽음도 로컬 사망 처리(OnDie)와 동일하게 이어지도록
        if (CurrentHp <= 0f)
        {
            Die();
            return;
        }

        // 호스트가 살아있다고 판정했으면 로컬 사망 상태도 함께 푼다 (구출 부활이 이 경로로 전파된다)
        if (IsDead)
        {
            IsDead = false;
            OnRevive?.Invoke();
        }
    }

    protected void RaiseHpChanged() => OnHpChanged?.Invoke(CurrentHp, MaxHp);
}
