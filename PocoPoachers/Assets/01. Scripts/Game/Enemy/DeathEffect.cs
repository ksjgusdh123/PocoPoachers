using UnityEngine;

public class DeathEffect : MonoBehaviour
{
    private StatBase _stat;

    private void Awake()
    {
        _stat = GetComponent<StatBase>();
        _stat.OnDie += PlayDeathEffect;
    }

    private void OnDestroy()
    {
        _stat.OnDie -= PlayDeathEffect;
    }

    private void PlayDeathEffect()
    {
        DeathVFXPool.Instance.Spawn(transform.position);
    }
}
