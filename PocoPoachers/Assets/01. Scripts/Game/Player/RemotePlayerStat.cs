public class RemotePlayerStat : StatBase
{
    public float Stamina { get; private set; }
    public float Hunger  { get; private set; }
    public float Thirst  { get; private set; }

    float _armorDefense;
    protected override float Defense => _armorDefense;

    public void SetVitalsFromNetwork(float stamina, float hunger, float thirst)
    {
        Stamina = stamina;
        Hunger  = hunger;
        Thirst  = thirst;
    }

    public void SetArmorDefense(float defense) => _armorDefense = defense;

    protected override void Awake()
    {
        base.Awake();
    }
}
