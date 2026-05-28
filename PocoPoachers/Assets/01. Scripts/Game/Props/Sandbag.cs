using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public class Sandbag : MonoBehaviour, IDamageable
{
    [SerializeField] private int _maxHits = 10;
    [SerializeField] private GameObject _rubblePrefab;
    [SerializeField] private float _destroyDelay = 2f;

    [SerializeField] private VisualEffect _vfx;

    private int _hitCount;

    public void TakeDamage(float damage, GameObject attacker = null)
    {
        _hitCount++;
        if (_hitCount >= _maxHits)
            HandleDie();
    }

    private void HandleDie()
    {
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        if (TryGetComponent<Collider>(out var col))
            col.enabled = false;

        if (_rubblePrefab != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 0.01f;
            Vector3 prefabEuler = _rubblePrefab.transform.rotation.eulerAngles;
            Quaternion rotation = Quaternion.Euler(prefabEuler.x, transform.eulerAngles.y, prefabEuler.z);
            Instantiate(_rubblePrefab, spawnPos, rotation);
        }

        if (_vfx != null)
        {
            _vfx.gameObject.SetActive(true);
            _vfx.SendEvent("OnPlay");
        }

        StartCoroutine(DestroyAfterDelay());
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(_destroyDelay);
        Destroy(gameObject);
    }
}
