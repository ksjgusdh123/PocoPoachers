using System;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManager : Singleton<ObjectManager>
{
    [Serializable]
    sealed class Entry
    {
        public ObjectKind Kind;
        public WorldObject Prefab;
    }

    struct PendingMove
    {
        public ObjectKind Kind;
        public int Id;
        public Vector3 Pos;
        public float Rotation;
        public sbyte MoveType;
        public int TypeId;
        public float VelocityX;
        public float VelocityZ;
        public bool IsSprinting;
        public bool IsRolling;
        public bool IsAiming;
        public bool IsReloading;
    }

    [SerializeField] Entry[] _entries;

    readonly Dictionary<(ObjectKind kind, int id), WorldObject> _objects = new();
    readonly Dictionary<ObjectKind, WorldObject> _prefabs = new();
    readonly List<H_ItemSpawnT> _spawnedBoxes = new();
    // 씬 배치 고정 오브젝트(창고 등). Clear()에서 Destroy하지 않는다.
    readonly HashSet<(ObjectKind kind, int id)> _sceneObjects = new();

    public IReadOnlyList<H_ItemSpawnT> SpawnedBoxes => _spawnedBoxes;
    public void RegisterSpawnedBox(H_ItemSpawnT data) => _spawnedBoxes.Add(data);
    // 비워져 디스폰된 박스를 late-join 게스트 스냅샷 대상에서 제거
    public void UnregisterSpawnedBox(int uid) => _spawnedBoxes.RemoveAll(b => b.Uid == uid);

    readonly object _moveLock = new object();
    readonly List<PendingMove> _pending = new();
    readonly List<PendingMove> _drain = new();

    protected override void Awake()
    {
        // NetworkManager/MainThreadDispatcher가 같은 GameObject에서 DontDestroyOnLoad를 걸기 때문에
        // ObjectManager도 덩달아 씬을 넘어 살아남는다. 그러면 매 씬마다 새로 배치된 ObjectManager는
        // 중복 싱글턴으로 인식되어 base.Awake()에서 곧바로 파괴되는데, 그 전에 이 씬의 _entries를
        // 살아남는 인스턴스로 넘겨줘야 씬별로 다른 프리팹 설정이 무시되지 않는다.
        if (_instance != null && _instance != this)
        {
            _instance.ApplySceneEntries(_entries);
            base.Awake(); // 중복 인스턴스이므로 여기서 gameObject가 파괴된다
            return;
        }

        base.Awake();
        CachePrefabs();
    }

#if UNITY_EDITOR
    void OnValidate() => CachePrefabs();
#endif

    void ApplySceneEntries(Entry[] entries)
    {
        _entries = entries;
        CachePrefabs();
    }

    void CachePrefabs()
    {
        _prefabs.Clear();
        if (_entries == null) return;
        foreach (var e in _entries)
        {
            if (e?.Prefab != null)
                _prefabs[e.Kind] = e.Prefab;
        }
    }

    public bool TryGet(ObjectKind kind, int id, out WorldObject obj)
    {
        var key = (kind, id);
        // 파괴된(씬 전환/퇴장) 오브젝트가 남아있으면 제거하고 없는 것으로 취급 — 호출부의 GetComponent 예외 방지
        if (_objects.TryGetValue(key, out obj))
        {
            if (obj != null) return true;
            _objects.Remove(key);
        }
        obj = null;
        return false;
    }

    public void Despawn(ObjectKind kind, int id)
    {
        var key = (kind, id);
        if (_objects.TryGetValue(key, out var obj) && obj != null)
            Destroy(obj.gameObject);
        _objects.Remove(key);
    }

    void Update()
    {
        _drain.Clear();
        lock (_moveLock)
        {
            if (_pending.Count == 0) return;
            _drain.AddRange(_pending);
            _pending.Clear();
        }
        foreach (var m in _drain)
            ApplyMove(m);
    }

    void Enqueue(PendingMove move)
    {
        lock (_moveLock)
            _pending.Add(move);
    }

    public void QueueMove(ObjectKind kind, int id, Vector3 pos, float rotation, sbyte moveType, float velX = 0f, float velZ = 0f, bool isSprinting = false, bool isRolling = false, bool isAiming = false, bool isReloading = false) =>
        Enqueue(new PendingMove { Kind = kind, Id = id, Pos = pos, Rotation = rotation, MoveType = moveType, VelocityX = velX, VelocityZ = velZ, IsSprinting = isSprinting, IsRolling = isRolling, IsAiming = isAiming, IsReloading = isReloading });


    void ApplyMove(in PendingMove m)
    {
        if (IsLocalPlayer(m.Kind, m.Id)) return;

        var key = (m.Kind, m.Id);
        if (_objects.TryGetValue(key, out var obj) && obj == null)
            _objects.Remove(key);

        if (!_objects.TryGetValue(key, out obj))
        {
            obj = CreateWorldObject(m.Kind, m.Id, m.TypeId);
            obj.transform.SetPositionAndRotation(m.Pos, Quaternion.Euler(0f, m.Rotation, 0f));
            _objects.Add(key, obj);
        }

        obj.SetMoveTarget(m.Pos, m.Rotation, m.VelocityX, m.VelocityZ, m.IsSprinting, m.IsRolling, m.IsAiming, m.IsReloading);
    }

    static bool IsLocalPlayer(ObjectKind kind, int id)
    {
        if (kind != ObjectKind.Player) return false;
        var nm = NetworkManager.Instance;
        return nm != null && id == nm.MyPlayerId;
    }

    // excluded(방금 죽은 플레이어)를 제외하고 살아있는(HP가 남은) 플레이어가 하나라도 있는지
    // 로컬 플레이어는 _objects에 없으므로(ApplyMove의 IsLocalPlayer) 별도로 확인한다
    public bool HasLivingPlayerExcept(StatBase excluded)
    {
        var local = FindFirstObjectByType<PlayerStat>();
        if (local != null && !ReferenceEquals(local, excluded) && local.CurrentHp > 0f)
            return true;

        foreach (var kv in _objects)
        {
            if (kv.Key.kind != ObjectKind.Player || kv.Value == null) continue;

            var stat = kv.Value.GetComponent<StatBase>();
            if (stat == null || ReferenceEquals(stat, excluded)) continue;
            if (stat.CurrentHp > 0f) return true;
        }
        return false;
    }

    // excluded를 제외하고 살아있는 플레이어의 WorldObject를 하나 반환 (없으면 null). 관전 대상 선택용.
    // 로컬 플레이어는 _objects에 없으므로 여기서 나오는 건 항상 원격 플레이어(동료)다.
    public WorldObject GetLivingPlayer(StatBase excluded)
    {
        foreach (var kv in _objects)
        {
            if (kv.Key.kind != ObjectKind.Player || kv.Value == null) continue;

            var stat = kv.Value.GetComponent<StatBase>();
            if (stat == null || ReferenceEquals(stat, excluded)) continue;
            if (stat.CurrentHp > 0f) return kv.Value;
        }
        return null;
    }

    public List<PlayerInfoT> GetAllPlayerInfos(int excludeId = -1)
    {
        var list = new List<PlayerInfoT>();
        foreach (var kv in _objects)
        {
            if (kv.Key.kind != ObjectKind.Player || kv.Key.id == excludeId) continue;
            var pos = kv.Value.transform.position;
            list.Add(new PlayerInfoT
            {
                PlayerId = kv.Key.id,
                Pos = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z },
                Rotation = kv.Value.transform.eulerAngles.y,
            });
        }
        return list;
    }

    public void Clear()
    {
        lock (_moveLock)
            _pending.Clear();

        var toDestroy = new List<WorldObject>();
        foreach (var kv in _objects)
        {
            if (_sceneObjects.Contains(kv.Key)) continue;
            if (kv.Value != null)
                toDestroy.Add(kv.Value);
        }

        foreach (var obj in toDestroy)
            Destroy(obj.gameObject);

        _objects.Clear();
        _sceneObjects.Clear();
        _spawnedBoxes.Clear();
    }

    // 씬에 미리 배치된 오브젝트를 네트워크 식별용으로 등록 (예: 쉘터 창고)
    // 호스트/게스트 양쪽에서 같은 고정 id로 등록해야 uid 기반 패킷이 서로 같은 오브젝트를 가리킨다
    public void RegisterSceneObject(ObjectKind kind, int id, GameObject go)
    {
        if (!go.TryGetComponent<WorldObject>(out var component))
            component = go.AddComponent<WorldObject>();
        component.Initialize(kind, id);
        var key = (kind, id);
        _objects[key] = component;
        _sceneObjects.Add(key);
    }

    public void UnregisterSceneObject(ObjectKind kind, int id)
    {
        var key = (kind, id);
        _sceneObjects.Remove(key);
        _objects.Remove(key);
        _spawnedBoxes.RemoveAll(box => box.Uid == id && kind == ObjectKind.ItemBox);
    }

    public ItemBox SpawnItemBox(int uid, int typeId, Vector3 pos, float rotation)
    {
        var obj = CreateWorldObject(ObjectKind.ItemBox, uid, typeId);
        obj.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotation, 0f));
        _objects[(ObjectKind.ItemBox, uid)] = obj;
        return obj.GetComponent<ItemBox>() ?? null;
    }

    WorldObject CreateWorldObject(ObjectKind kind, int id, int typeId = 0)
    {
        GameObject prefab = null;

        if (typeId > 0 && (kind == ObjectKind.ItemBox || kind == ObjectKind.WorldItem))
        {
            var data = ItemTable.Instance.Get(typeId);
            if (data != null)
                prefab = ResourceManager.Instance.Load<GameObject>(data.prefab);
        }

        if (prefab == null && _prefabs.TryGetValue(kind, out var fallbackPrefab))
            prefab = fallbackPrefab.gameObject;

        GameObject go = prefab != null ? Instantiate(prefab) : CreateFallback();
        go.name = $"{kind}_{id}";

        if (!go.TryGetComponent<WorldObject>(out var component))
            component = go.AddComponent<WorldObject>();

        component.Initialize(kind, id, typeId);

        // 원격 플레이어는 생성 즉시 스탯을 붙여야 첫 StatSync 도착 전에도 총알 피격(IDamageable)이 가능하다
        if (kind == ObjectKind.Player)
        {
            if (!go.TryGetComponent<StatBase>(out _))
                go.AddComponent<RemotePlayerStat>();

            // 원격 플레이어 발소리 방출 — 프리팹에 미리 붙여 값을 조정했다면 그것을 쓰고, 없으면 기본값으로 부착
            if (!go.TryGetComponent<RemoteFootstepEmitter>(out _))
                go.AddComponent<RemoteFootstepEmitter>();

            // Equip 패킷이 스폰보다 먼저 도착했을 수 있다 (씬 전환 시 장비 복원이 첫 Move보다 빠름)
            RemoteEquipState.ApplyTo(component, id);
        }

        return component;
    }

    static GameObject CreateFallback()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(go.GetComponent<Collider>());
        return go;
    }
}
