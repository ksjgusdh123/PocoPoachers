using System;
using UnityEngine;

// 원격 플레이어의 스탯 사본 — PlayerStat과 같은 스탯 구성(HP/스태미나/배터리)을 갖되
// 값은 로컬 시뮬레이션 없이 네트워크 동기화로만 갱신된다
public class RemotePlayerStat : StatBase
{
    [SerializeField] private float _maxHp = 100f;
    [SerializeField] private float _maxStamina = 100f;
    [SerializeField] private float _maxBattery = 100f;

    // 구출용 트리거 자식 오브젝트 — 평소엔 꺼두고 다운 시에만 켠다 (프리팹에서 지정)
    [SerializeField] private RescueInteractable _rescueTrigger;

    public float MaxStamina { get; private set; }
    public float CurrentStamina { get; private set; }

    public float MaxBattery { get; private set; }
    public float CurrentBattery { get; private set; }

    public event Action<float, float> OnStaminaChanged;
    public event Action<float, float> OnBatteryChanged;

    float _armorDefenseRate;
    float _armorMaxHpBonus;
    float _armorMoveSpeedMultiplier = 1f;

    protected override float DefenseRate => _armorDefenseRate + DefenseBuffRate;

    public float ArmorMoveSpeedMultiplier => _armorMoveSpeedMultiplier;
    public float ArmorDefenseRate => _armorDefenseRate;

    protected override void Awake()
    {
        base.Awake();

        // PlayerStat과 동일한 기본값으로 시작 — 첫 동기화 전까지 HP 0(사망/피격 불가 취급)이 되는 것을 방지
        MaxHp = _maxHp;
        CurrentHp = MaxHp;
        MaxStamina = _maxStamina;
        CurrentStamina = MaxStamina;
        MaxBattery = _maxBattery;
        CurrentBattery = MaxBattery;

        // 프리팹에 켜진 채로 저장돼도 살아있는 플레이어가 구출 대상으로 보이지 않도록 강제로 끈다
        if (_rescueTrigger != null)
            _rescueTrigger.gameObject.SetActive(false);

        // 데미지는 호스트에서만 적용되므로(Bullet._applyDamage) 게스트의 전투 사망은 호스트의 이 컴포넌트에서 감지된다
        OnDie += HandleRemoteDeath;
        OnRevive += HandleRemoteRevive;
    }

    public void ApplyNetworkStats(float hp, float maxHp, float stamina, float battery, float defense, float critMultiplier, float rangeMultiplier, float luckyChance = StatBase.DefaultLuckyShotChance, float luckyMultiplier = StatBase.DefaultLuckyShotMultiplier, float attackPowerMultiplier = StatBase.DefaultAttackPowerMultiplier, float defenseBuffRate = StatBase.DefaultDefenseBuffRate)
    {
        CritMultiplier = critMultiplier;
        RangeMultiplier = rangeMultiplier;
        LuckyShotChance = luckyChance;
        LuckyShotMultiplier = luckyMultiplier;
        AttackPowerMultiplier = attackPowerMultiplier;
        DefenseBuffRate = defenseBuffRate;
        _armorMaxHpBonus = 0f;
        _armorMoveSpeedMultiplier = 1f;
        SetHpFromNetwork(hp, maxHp, 0);
        SetVitalsFromNetwork(stamina, battery);
        SetArmorDefenseRate(defense);
    }

    public void SetVitalsFromNetwork(float stamina, float battery)
    {
        // 강화 등으로 소유자의 최대치가 100을 넘을 수 있는데 패킷엔 현재값만 오므로, 관측된 최대값으로 보정
        MaxStamina = Mathf.Max(MaxStamina, stamina);
        MaxBattery = Mathf.Max(MaxBattery, battery);

        CurrentStamina = stamina;
        CurrentBattery = battery;
        OnStaminaChanged?.Invoke(CurrentStamina, MaxStamina);
        OnBatteryChanged?.Invoke(CurrentBattery, MaxBattery);
    }

    public void SetArmorDefenseRate(float defenseRate) => _armorDefenseRate = defenseRate;

    public override void ApplyArmorStat(ArmorStatData data)
    {
        base.ApplyArmorStat(data);
        _armorDefenseRate = _totalDefenseRate;
        _armorMaxHpBonus += data.MaxHpBonus;
        if (data.MoveSpeedMultiplier > 0f)
            _armorMoveSpeedMultiplier *= data.MoveSpeedMultiplier;

        MaxHp += data.MaxHpBonus;
        CurrentHp = Mathf.Min(CurrentHp + data.MaxHpBonus, MaxHp);
        RaiseHpChanged();
    }

    public override void RemoveArmorStat(ArmorStatData data)
    {
        base.RemoveArmorStat(data);
        _armorDefenseRate = _totalDefenseRate;
        _armorMaxHpBonus = Mathf.Max(0f, _armorMaxHpBonus - data.MaxHpBonus);
        if (data.MoveSpeedMultiplier > 0f)
            _armorMoveSpeedMultiplier /= data.MoveSpeedMultiplier;

        MaxHp = Mathf.Max(1f, MaxHp - data.MaxHpBonus);
        CurrentHp = Mathf.Min(CurrentHp, MaxHp);
        RaiseHpChanged();
    }

    public void ApplyConsumableEffect(ItemData data)
    {
        if (data == null) return;

        switch (data.EffectType)
        {
            case EffectType.HP:
                Heal(data.effect_value);
                break;
            case EffectType.Thirst:
                CurrentBattery = Mathf.Min(MaxBattery, CurrentBattery + data.effect_value);
                OnBatteryChanged?.Invoke(CurrentBattery, MaxBattery);
                break;
        }
    }

    // 호스트에서 총알/힐이 이 원격 플레이어에 적용됐을 때(TakeDamage/Heal) 판정 결과를 모든 게스트에 전파
    // 소유 게스트가 이걸 받아 자기 로컬 PlayerStat에 반영해야 HP 권한이 호스트로 통일된다
    // (게스트 자기보고 수신은 SetHpFromNetwork 경로라 여기를 타지 않음 — 에코 루프 없음)
    protected override void OnLocalHpChanged(float hp, float maxHp)
    {
        if (!RoomManager.IsHost) return;
        if (!TryGetComponent<WorldObject>(out var worldObj)) return;

        Debug.Log($"[RemotePlayerStat] 플레이어 {worldObj.Id} HP {hp}/{maxHp} 판정 → 게스트 전파");
        PacketBuilder.BroadcastReliableToGuests(new H_StatSyncT
        {
            PlayerId = worldObj.Id,
            Hp       = hp,
            MaxHp    = maxHp,
            Stamina  = CurrentStamina,
            Battery  = CurrentBattery,
            Defense  = _armorDefenseRate,
        }, H_StatSync.Pack, PacketType.H_StatSync);
    }

    // 원격 플레이어 사망 감지 — 호스트는 TakeDamage에서, 게스트는 SetHpFromNetwork(HP 0 수신)에서 발동
    // 살아있는 다른 플레이어가 있으면 이 사본을 구출(F 상호작용) 대상으로 전환 (게스트 상자 스폰은 아직 미지원)
    private void HandleRemoteDeath()
    {
        if (ObjectManager.Instance == null || !ObjectManager.Instance.HasLivingPlayerExcept(this)) return;

        Debug.Log("구출!");
        SetRescueTriggerActive(true);
    }

    // 구출로 되살아나면 더 이상 구출 대상이 아니다 — 호스트/게스트 모두 이 경로로 트리거를 내린다
    private void HandleRemoteRevive() => SetRescueTriggerActive(false);

    public void SetRescueTriggerActive(bool active)
    {
        if (_rescueTrigger == null)
        {
            if (active)
                Debug.LogWarning($"[RemotePlayerStat] {name}에 구출 트리거가 지정되지 않아 구출할 수 없습니다", this);
            return;
        }

        _rescueTrigger.gameObject.SetActive(active);
    }
}
