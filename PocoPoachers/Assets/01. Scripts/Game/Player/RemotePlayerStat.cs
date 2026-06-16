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

    protected override void Awake()
    {
        base.Awake();
    }
}
