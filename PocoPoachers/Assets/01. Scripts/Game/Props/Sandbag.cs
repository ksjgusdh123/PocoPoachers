using UnityEngine;

public class Sandbag : StatBase
{
    [SerializeField] private float _maxHp = 200f;

    protected override void Awake()
    {
        base.Awake();
        MaxHp = _maxHp;
        CurrentHp = _maxHp;
        OnDie += HandleDie;
    }

    private void HandleDie()
    {
        gameObject.SetActive(false);
    }
}
