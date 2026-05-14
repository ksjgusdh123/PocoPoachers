using UnityEngine;

public class EnemyStat : StatBase
{
    [SerializeField] private int _monsterId;

    protected override void Awake()
    {
        base.Awake();
        MaxHp = 100f;
        CurrentHp = 100f;
    }

    private void Start()
    {
    }

    public void Initialize()
    {

    }
}
