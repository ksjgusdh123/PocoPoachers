public class RemotePlayerStat : StatBase
{
    public float Stamina { get; private set; }
    public float Hunger  { get; private set; }
    public float Thirst  { get; private set; }

    float _armorDefenseRate;
    protected override float DefenseRate => _armorDefenseRate;

    public void SetVitalsFromNetwork(float stamina, float hunger, float thirst)
    {
        Stamina = stamina;
        Hunger  = hunger;
        Thirst  = thirst;
    }

    public void SetArmorDefenseRate(float defenseRate) => _armorDefenseRate = defenseRate;

    protected override void Awake()
    {
        base.Awake();
    }
}
