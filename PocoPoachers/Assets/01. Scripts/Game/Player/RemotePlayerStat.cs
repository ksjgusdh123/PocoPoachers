using UnityEngine;

public class RemotePlayerStat : StatBase
{
    public float Stamina { get; private set; }
    public float Battery { get; private set; }

    float _armorDefenseRate;
    float _armorMaxHpBonus;
    float _armorMoveSpeedMultiplier = 1f;

    protected override float DefenseRate => _armorDefenseRate;

    public float ArmorMoveSpeedMultiplier => _armorMoveSpeedMultiplier;
    public float ArmorDefenseRate => _armorDefenseRate;

    public void ApplyNetworkStats(float hp, float maxHp, float stamina, float battery, float defense)
    {
        _armorMaxHpBonus = 0f;
        _armorMoveSpeedMultiplier = 1f;
        SetHpFromNetwork(hp, maxHp, 0);
        SetVitalsFromNetwork(stamina, battery);
        SetArmorDefenseRate(defense);
    }

    public void SetVitalsFromNetwork(float stamina, float battery)
    {
        Stamina = stamina;
        Battery = battery;
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
            case EffectType.Hunger:
            case EffectType.Thirst:
                Battery = Mathf.Min(200f, Battery + data.effect_value);
                break;
            case EffectType.Stamina:
                Stamina = Mathf.Min(200f, Stamina + data.effect_value);
                break;
        }
    }

    protected override void Awake()
    {
        base.Awake();
    }
}
