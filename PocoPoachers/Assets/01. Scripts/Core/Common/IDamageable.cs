using UnityEngine;

public interface IDamageable
{
    bool TakeDamage(float damage, GameObject attacker = null);
}
