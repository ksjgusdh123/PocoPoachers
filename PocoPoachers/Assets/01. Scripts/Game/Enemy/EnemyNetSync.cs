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
    int     _netAnimState = -1;

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
        int animState = _animator != null ? _animator.GetInteger(AnimStateHash) : 0;
        RoomSync.EnemyMove(EnemyId, transform.position, transform.eulerAngles.y, animState);
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
            int animState = _netAnimState >= 0
                ? _netAnimState
                : (distToTarget > WalkThreshold ? (int)AIAnimState.Walk : (int)AIAnimState.Idle);
            _animator.SetInteger(AnimStateHash, animState);
        }
    }

    GameObject _lastAttacker; // 마지막으로 이 적을 때린 대상 (킬 집계용)

    void OnHostDamaged(float damage, Vector3 pos, GameObject attacker)
    {
        if (_stat == null) return;
        if (attacker != null) _lastAttacker = attacker;
        RoomSync.EnemyHit(EnemyId, _stat.CurrentHp, _stat.MaxHp, damage);
    }

    void OnHostDie()
    {
        int killerId = ResolveKillerPlayerId(_lastAttacker);

        // 호스트 자신이 처치했으면 로컬 집계 (게스트는 H_EnemyDie의 killerId로 각자 집계)
        if (killerId != 0 && killerId == (NetworkManager.Instance?.MyPlayerId ?? -1))
            RaidStats.Instance.AddKill();

        RoomSync.EnemyDie(EnemyId, killerId);
    }

    // 적을 처치한 총알의 발사자 playerId를 구한다.
    // 로컬(호스트) 플레이어는 PlayerController 보유 → 내 id, 게스트는 원격 WorldObject → 그 Id.
    int ResolveKillerPlayerId(GameObject attacker)
    {
        if (attacker == null) return 0;
        if (attacker.TryGetComponent<PlayerController>(out _))
            return NetworkManager.Instance?.MyPlayerId ?? 0;
        if (attacker.TryGetComponent<WorldObject>(out var wo) && wo.Kind == ObjectKind.Player)
            return wo.Id;
        return 0;
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

    public static void OnNetMove(int id, Vector3 pos, float rotation, int animState)
    {
        if (!_registry.TryGetValue(id, out var e) || e == null) return;
        e._targetPos    = pos;
        e._targetYaw    = rotation;
        e._hasTarget    = true;
        e._netAnimState = animState;
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

    // 게스트가 적 총알을 스폰할 때 attacker(=적 오브젝트)로 쓰기 위한 조회 — 레이어가 Enemy라 적끼리 오사가 안 난다
    public static bool TryGetGameObject(int id, out GameObject go)
    {
        go = null;
        if (!_registry.TryGetValue(id, out var e) || e == null) return false;
        go = e.gameObject;
        return true;
    }

    // 호스트가 전파한 AI 대사 — 게스트가 자기 쪽 같은 적을 찾아 말풍선 표시 (표시 판정은 각자 시야로)
    public static void OnNetSpeak(int id, string message, float duration)
    {
        if (!_registry.TryGetValue(id, out var e) || e == null) return;
        var speech = e.GetComponentInChildren<AISpeech>();
        if (speech != null)
            speech.ShowRemote(message, duration);
    }
}
