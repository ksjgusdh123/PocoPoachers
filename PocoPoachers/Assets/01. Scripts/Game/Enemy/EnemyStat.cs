using UnityEngine;

public class EnemyStat : StatBase
{
    [SerializeField] private int _monsterId;

    private void Awake()
    {
        MaxHp = 100f;
        CurrentHp = 100f;
        OnDamaged += (damage, pos) => DamageTextPool.Show(damage, pos);
        //InvokeRepeating(nameof(TestDamage), 2f, 0.5f);
    }

    private void Start()
    {
        Debug.Log("[EnemyStat] Start 호출됨, 3초 후 사망 예정");
        Invoke(nameof(TestDie), 3f);
    }

    private void TestDie()
    {
        Debug.Log("[EnemyStat] TestDie 호출됨");
        TakeDamage(MaxHp);
    }

    //private void TestDamage() => TakeDamage(1f);

    public void Initialize()
    {

    }
}
