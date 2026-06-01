using System.Collections.Generic;
using UnityEngine;

public class EnemyNetSync : MonoBehaviour
{
    static readonly Dictionary<int, EnemyNetSync> _registry = new();
    static int _nextId = 1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetCounter() { _nextId = 1; _registry.Clear(); }

    [SerializeField] Behaviour[] _aiComponents;
    [SerializeField] float _syncInterval = 0.1f;

    public int EnemyId { get; private set; }

    EnemyStat _stat;
    float _timer;

    Vector3 _targetPos;
    float   _targetYaw;
    bool    _hasTarget;

    void Awake()
    {
        EnemyId = _nextId++;
        _registry[EnemyId] = this;
        _stat = GetComponent<EnemyStat>();
    }

    void Start()
    {
        if (RoomManager.IsHost)
        {
            if (_stat != null)
            {
                _stat.OnDamaged += OnHostDamaged;
                _stat.OnDie     += OnHostDie;
            }
        }
        else
        {
            foreach (var c in _aiComponents)
                if (c != null) c.enabled = false;
        }
    }

    void OnDestroy()
    {
        _registry.Remove(EnemyId);
        if (_stat != null)
        {
            _stat.OnDamaged -= OnHostDamaged;
            _stat.OnDie     -= OnHostDie;
        }
    }

    void Update()
    {
        if (RoomManager.IsHost)
            UpdateHost();
        else
            UpdateGuest();
    }

    void UpdateHost()
    {
        if (!RoomManager.HasGuests) return;
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = _syncInterval;
        RoomSync.EnemyMove(EnemyId, transform.position, transform.eulerAngles.y);
    }

    void UpdateGuest()
    {
        if (!_hasTarget) return;
        float t = 1f - Mathf.Exp(-14f * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, _targetPos, t);
        float y = Mathf.LerpAngle(transform.eulerAngles.y, _targetYaw, t);
        transform.rotation = Quaternion.Euler(0f, y, 0f);
    }

    void OnHostDamaged(float damage, Vector3 pos, GameObject attacker)
    {
        if (_stat == null) return;
        RoomSync.EnemyHit(EnemyId, _stat.CurrentHp, _stat.MaxHp);
    }

    void OnHostDie()
    {
        RoomSync.EnemyDie(EnemyId);
    }

    public static void SendAllToGuest(int guestPlayerId)
    {
        foreach (var kv in _registry)
        {
            var e = kv.Value;
            if (e == null) continue;
            RoomSync.EnemySpawnToGuest(
                guestPlayerId, e.EnemyId,
                e.transform.position, e.transform.eulerAngles.y,
                e._stat?.CurrentHp ?? 100f, e._stat?.MaxHp ?? 100f);
        }
    }

    public static void OnNetSpawn(int id, Vector3 pos, float rotation, float hp, float maxHp)
    {
        if (!_registry.TryGetValue(id, out var e) || e == null) return;
        e.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotation, 0f));
        e._targetPos = pos;
        e._targetYaw = rotation;
        e._hasTarget = true;
        e._stat?.SetHpFromNetwork(hp, maxHp);
    }

    public static void OnNetMove(int id, Vector3 pos, float rotation)
    {
        if (!_registry.TryGetValue(id, out var e) || e == null) return;
        e._targetPos = pos;
        e._targetYaw = rotation;
        e._hasTarget = true;
    }

    public static void OnNetHit(int id, float hp, float maxHp)
    {
        if (!_registry.TryGetValue(id, out var e) || e == null) return;
        e._stat?.SetHpFromNetwork(hp, maxHp);
    }

    public static void OnNetDie(int id)
    {
        if (!_registry.TryGetValue(id, out var e) || e == null) return;
        _registry.Remove(id);
        DeathVFXPool.Instance?.Spawn(e.transform.position);
        Destroy(e.gameObject);
    }
}
