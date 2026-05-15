using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float damage, GameObject attacker = null);
}
