using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class Sandbag : MonoBehaviour, IDamageable
{
    [SerializeField] private int _maxHits = 10;
    [SerializeField] private GameObject _rubblePrefab;
    [SerializeField] private float _destroyDelay = 2f;
    [SerializeField] private int _presetNetworkId;

    [SerializeField] private VisualEffect _vfx;

    private int _hitCount;
    private int _networkId;

    private static int _nextNetworkId = 1;
    private static readonly Dictionary<int, Sandbag> _registry = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics()
    {
        _nextNetworkId = 1;
        _registry.Clear();
    }

    public static Sandbag Find(int networkId) => _registry.TryGetValue(networkId, out var s) ? s : null;

    private void Awake()
    {
        _networkId = _presetNetworkId > 0 ? _presetNetworkId : StableIdFromPosition(transform.position);
        if (_registry.ContainsKey(_networkId))
            _networkId = _nextNetworkId++;

        _registry[_networkId] = this;
    }

    static int StableIdFromPosition(Vector3 position)
    {
        unchecked
        {
            int x = Mathf.RoundToInt(position.x * 100f);
            int y = Mathf.RoundToInt(position.y * 100f);
            int z = Mathf.RoundToInt(position.z * 100f);
            return (x * 73856093) ^ (y * 19349663) ^ (z * 83492791);
        }
    }

    private void OnDestroy()
    {
        _registry.Remove(_networkId);
    }

    public bool TakeDamage(float damage, GameObject attacker = null)
    {
        _hitCount++;
        if (_hitCount >= _maxHits)
            HandleDie();
        return true;
    }

    private void HandleDie()
    {
        PlayDestroyEffects();

        if (RoomManager.IsHost && RoomManager.HasGuests)
            PacketBuilder.BroadcastToGuests(
                new H_SandbagDestroyT { SandbagId = _networkId },
                H_SandbagDestroy.Pack, PacketType.H_SandbagDestroy);

        StartCoroutine(DestroyAfterDelay());
    }

    public void DestroyFromNetwork()
    {
        PlayDestroyEffects();
        StartCoroutine(DestroyAfterDelay());
    }

    private void PlayDestroyEffects()
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
    }

    private IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(_destroyDelay);
        Destroy(gameObject);
    }
}
