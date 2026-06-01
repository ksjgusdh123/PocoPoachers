public class RemotePlayerStat : StatBase
{
    public float Stamina { get; private set; }
    public float Hunger  { get; private set; }
    public float Thirst  { get; private set; }

    public void SetVitalsFromNetwork(float stamina, float hunger, float thirst)
    {
        Stamina = stamina;
        Hunger  = hunger;
        Thirst  = thirst;
    }

    protected override void Awake()
    {
        base.Awake();
    }
}
