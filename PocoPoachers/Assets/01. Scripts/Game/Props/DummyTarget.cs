using System.Collections;
using UnityEngine;

public class DummyTarget : StatBase
{
    [SerializeField] private float _maxHp = 1000f;
    [SerializeField] private float _reviveDelay = 3f;

    protected override void Awake()
    {
        base.Awake();

        MaxHp = _maxHp;
        CurrentHp = MaxHp;

        OnDie += () => StartCoroutine(ReviveAfterDelay());
    }

    private IEnumerator ReviveAfterDelay()
    {
        yield return new WaitForSeconds(_reviveDelay);
        Revive(MaxHp);
    }
}
