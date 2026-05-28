using UnityEngine;

public class Sandbag : MonoBehaviour, IDamageable
{
    [SerializeField] private int _maxHits = 10;
    private int _hitCount;

    public void TakeDamage(float damage, GameObject attacker = null)
    {
        _hitCount++;
        if (_hitCount >= _maxHits)
            gameObject.SetActive(false);
    }
}
