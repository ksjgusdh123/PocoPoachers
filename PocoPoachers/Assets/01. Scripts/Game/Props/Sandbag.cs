using UnityEngine;

public class Sandbag : MonoBehaviour, IDamageable
{
    [SerializeField] private int _maxHits = 10;
    [SerializeField] private GameObject _destroyVfxPrefab;
    [SerializeField] private GameObject _rubblePrefab;
    private int _hitCount;

    public void TakeDamage(float damage, GameObject attacker = null)
    {
        _hitCount++;
        if (_hitCount >= _maxHits)
            HandleDie();
    }

    private void HandleDie()
    {
        if (_destroyVfxPrefab != null)
            Instantiate(_destroyVfxPrefab, transform.position, Quaternion.identity);
        if (_rubblePrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.01f;
            Instantiate(_rubblePrefab, spawnPos, _rubblePrefab.transform.rotation);
        }
        gameObject.SetActive(false);
    }
}
