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
    public int EnemyTypeId => _stat?.EnemyId ?? 0;

    EnemyStat _stat;
    AIWeaponController _weaponController;
    ArmorController _armorController;
    ArmorMount _armorMount;
    Animator _animator;
    float _timer;

    Vector3 _targetPos;
    float   _targetYaw;
    bool    _hasTarget;

    static readonly int AnimStateHash = Animator.StringToHash("currentState");
    const float WalkThreshold = 0.05f;

    void Awake()
    {
        EnemyId = _nextId++;
        _registry[EnemyId] = this;
        _stat = GetComponent<EnemyStat>();
        _weaponController = GetComponent<AIWeaponController>();
        _armorController = GetComponent<ArmorController>();
        _armorMount = GetComponent<ArmorMount>();
        _animator = GetComponentInChildren<Animator>();
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

    // 호스트가 보내준 EnemyId로 등록을 갱신 (게스트가 패킷으로 받은 적을 새로 스폰했을 때 사용)
    void AssignNetworkId(int id)
    {
        _registry.Remove(EnemyId);
        EnemyId = id;
        _registry[EnemyId] = this;
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

        float distToTarget = Vector3.Distance(transform.position, _targetPos);
        transform.position = Vector3.Lerp(transform.position, _targetPos, t);
        float y = Mathf.LerpAngle(transform.eulerAngles.y, _targetYaw, t);
        transform.rotation = Quaternion.Euler(0f, y, 0f);

        if (_animator != null)
        {
            int animState = distToTarget > WalkThreshold ? (int)AIAnimState.Walk : (int)AIAnimState.Idle;
            _animator.SetInteger(AnimStateHash, animState);
        }
    }

    void OnHostDamaged(float damage, Vector3 pos, GameObject attacker)
    {
        if (_stat == null) return;
        RoomSync.EnemyHit(EnemyId, _stat.CurrentHp, _stat.MaxHp, damage);
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
            int weaponId = e._weaponController?.Gun?.ItemId ?? 0;
            int helmetId = e._armorMount?.GetEquippedItemId() ?? 0;
            RoomSync.EnemySpawnToGuest(
                guestPlayerId, e.EnemyTypeId, e.EnemyId,
                e.transform.position, e.transform.eulerAngles.y,
                e._stat?.CurrentHp ?? 100f, e._stat?.MaxHp ?? 100f,
                weaponId, helmetId);
        }
    }

    public static void OnNetSpawn(int id, int enemyTypeId, Vector3 pos, float rotation, float hp, float maxHp, int weaponId, int helmetId)
    {
        if (!_registry.TryGetValue(id, out var e) || e == null)
        {
            e = SpawnFromType(enemyTypeId, id, pos, rotation);
            if (e == null) return;
        }

        e.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotation, 0f));
        e._targetPos = pos;
        e._targetYaw = rotation;
        e._hasTarget = true;
        e._stat?.SetHpFromNetwork(hp, maxHp, 0);

        if (weaponId != 0)
            e._weaponController?.EquipGun(weaponId);

        if (helmetId != 0)
        {
            var itemData = ItemTable.Instance.Get(helmetId);
            if (itemData != null)
                e._armorController?.Equip(itemData, 0, 0);
        }
    }

    static EnemyNetSync SpawnFromType(int enemyTypeId, int id, Vector3 pos, float rotation)
    {
        var spawner = Object.FindFirstObjectByType<EnemySpawner>();
        if (spawner == null) return null;

        var go = spawner.SpawnEnemyByTypeId(enemyTypeId, pos, Quaternion.Euler(0f, rotation, 0f));
        if (go == null) return null;

        var sync = go.GetComponent<EnemyNetSync>();
        if (sync == null) return null;

        sync.AssignNetworkId(id);
        return sync;
    }

    public static void OnNetMove(int id, Vector3 pos, float rotation)
    {
        if (!_registry.TryGetValue(id, out var e) || e == null) return;
        e._targetPos = pos;
        e._targetYaw = rotation;
        e._hasTarget = true;
    }

    public static void OnNetHit(int id, float hp, float maxHp, float damage)
    {
        if (!_registry.TryGetValue(id, out var e) || e == null) return;
        e._stat?.SetHpFromNetwork(hp, maxHp, damage);
    }

    public static void OnNetDie(int id)
    {
        if (!_registry.TryGetValue(id, out var e) || e == null) return;
        _registry.Remove(id);
        DeathVFXPool.Instance?.Spawn(e.transform.position);
        Destroy(e.gameObject);
    }
}
