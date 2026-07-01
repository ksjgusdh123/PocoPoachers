using UnityEngine;

public class RemotePlayerStat : StatBase
{
    public float Stamina { get; private set; }
    public float Battery { get; private set; }

    float _armorDefenseRate;
    protected override float DefenseRate => _armorDefenseRate;

    public void SetVitalsFromNetwork(float stamina, float battery)
    {
        Stamina = stamina;
        Battery = battery;
    }

    public void SetArmorDefenseRate(float defenseRate) => _armorDefenseRate = defenseRate;
    public float ArmorDefenseRate => _armorDefenseRate;

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
