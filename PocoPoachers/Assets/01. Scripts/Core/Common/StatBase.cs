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

    // 크리티컬(현재는 헤드샷) 데미지 배율. 방어율처럼 캐릭터 스탯이라 스킬·강화·장비 어디서 올려도 된다.
    // 데미지는 호스트가 넣으므로, 게스트 값은 StatSync를 타고 호스트의 RemotePlayerStat까지 가야 반영된다.
    public const float DefaultCritMultiplier = 2f;
    public float CritMultiplier { get; set; } = DefaultCritMultiplier;

    // 탄환 사거리 배율. 크리 배율과 같은 경로로 호스트까지 간다.
    public const float DefaultRangeMultiplier = 1f;
    public float RangeMultiplier { get; set; } = DefaultRangeMultiplier;

    // 은신 상태 — 켜져 있으면 적 AI 탐지 후보에서 제외된다(TargetDetector 참고).
    // 원인이 스킬 하나뿐이라 무적처럼 자동 전파하지 않고, 호출부(StealthSkill)가 RoomSync.Stealth로 명시 전파한다.
    public bool IsStealthed { get; set; }

    // 게스트의 은신 여부 — 탐지는 호스트만 판정하므로(TargetDetector가 호스트 전용) 이 값으로만 안다.
    private bool _networkStealthed;
    public void ApplyStealthFromNetwork(bool value) => _networkStealthed = value;

    // 탐지 회피 여부의 정본 — TargetDetector는 이것만 보면 된다(로컬/원격 어느 쪽 원인이든)
    public bool IsUndetectable => IsStealthed || _networkStealthed;

    // 무적 상태 (구르기 등에서 켜고 끔) — 켜져 있으면 데미지를 받지 않음
    public bool IsInvincible { get; private set; }

    // 다른 클라이언트가 "이 대상은 지금 피해 면역"이라고 알려준 값.
    // 원격 대상은 무적을 건 주체가 여기 없으므로(적 AI는 호스트 전용, 치트는 호스트 전용) 이 값으로만 안다.
    private bool _networkImmune;

    // 피해 면역 여부의 정본. 무적 원인이 늘어나도 판정하는 쪽은 이것만 보면 된다.
    // 총알 관통·혈흔·히트마커가 전부 이 하나로 갈리므로, 원인별로 따로 검사하면 반드시 어딘가 빠진다.
    public bool IsDamageImmune
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (IsGodMode) return true;
#endif
            return IsInvincible || _networkImmune;
        }
    }

    // 무적을 거는 입구. 호출부는 네트워크를 몰라도 되고, 어떤 효과가 걸든 자동으로 전파된다.
    public void SetInvincible(bool value)
    {
        if (IsInvincible == value) return;

        IsInvincible = value;
        NotifyImmunityChanged();
    }

    // 네트워크로 받은 면역 상태를 반영 (되돌려 보내지 않는다)
    public void ApplyImmunityFromNetwork(bool value) => _networkImmune = value;

    // 마지막으로 알린 면역 상태 — 원인(무적/치트)이 여럿이라 실제로 바뀔 때만 보내기 위해 기억한다
    private bool _notifiedImmune;

    protected void NotifyImmunityChanged()
    {
        bool immune = IsDamageImmune;
        if (_notifiedImmune == immune) return;

        _notifiedImmune = immune;
        RoomSync.Invincible(this, immune);
    }

    // 반사 상태 (반사 스킬에서 켜고 끔) — 켜져 있으면 무적으로 막힌 총알이 Bullet.cs에서
    // 관통 대신 역벡터로 반사된다. 반사 스킬은 항상 SetInvincible(true)도 함께 걸므로
    // 데미지 면역 자체는 IsDamageImmune 경로를 그대로 탄다 — 이건 "막힌 총알을 어떻게 처리할지"만 결정한다.
    public bool IsReflecting { get; private set; }

    // 다른 클라이언트가 "이 대상은 지금 반사 중"이라고 알려준 값 — 반사를 건 스킬은 로컬에서만 돌아서
    // 원격 대상(호스트가 보는 게스트 등)은 이 값으로만 안다.
    private bool _networkReflecting;

    // 반사 여부의 정본 — Bullet.cs가 무적으로 막힌 순간 이것만 보면 된다.
    public bool IsBulletReflecting => IsReflecting || _networkReflecting;

    public void SetReflecting(bool value)
    {
        if (IsReflecting == value) return;

        IsReflecting = value;
        RoomSync.Reflecting(this, value);
    }

    // 네트워크로 받은 반사 상태를 반영 (되돌려 보내지 않는다)
    public void ApplyReflectingFromNetwork(bool value) => _networkReflecting = value;

    // 행운의 사격 확률/배율 (행운의 사격 스킬에서 켜고 끔) — 켜져 있는 동안 명중할 때마다 이 확률로
    // 이번 탄환의 데미지가 배율만큼 오른다. 크리/사거리 배율과 같은 경로로 StatSync를 타고 호스트까지
    // 가지만, 실제 굴림(Random)은 데미지를 넣는 호스트가 Bullet.cs에서 공격자 스탯을 보고 직접 한다 —
    // 게스트가 굴린 결과를 신뢰하는 게 아니라 확률/배율이라는 "값"만 신뢰한다(크리 배율과 동일한 신뢰 모델).
    public const float DefaultLuckyShotChance = 0f;
    public const float DefaultLuckyShotMultiplier = 1f;
    public float LuckyShotChance { get; set; } = DefaultLuckyShotChance;
    public float LuckyShotMultiplier { get; set; } = DefaultLuckyShotMultiplier;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    // 치트 무적 — 구르기 무적과 별개로 유지되어 구르기 종료에 꺼지지 않음
    public bool IsGodMode { get; private set; }

    public void SetGodMode(bool value)
    {
        if (IsGodMode == value) return;

        IsGodMode = value;
        NotifyImmunityChanged();   // 치트도 같은 경로로 전파해야 게스트 총알이 관통한다
    }
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
        if (IsDamageImmune) return false;
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
