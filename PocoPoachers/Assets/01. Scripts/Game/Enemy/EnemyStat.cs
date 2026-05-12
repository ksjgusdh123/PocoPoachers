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

    //private void TestDamage() => TakeDamage(1f);

    public void Initialize()
    {

    }
}
