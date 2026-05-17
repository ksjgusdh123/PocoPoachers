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
    }

    [SerializeField] Entry[] _entries;

    readonly Dictionary<(ObjectKind kind, int id), WorldObject> _objects = new();
    readonly Dictionary<ObjectKind, WorldObject> _prefabs = new();
    readonly List<H_ItemSpawnT> _spawnedBoxes = new();

    public IReadOnlyList<H_ItemSpawnT> SpawnedBoxes => _spawnedBoxes;
    public void RegisterSpawnedBox(H_ItemSpawnT data) => _spawnedBoxes.Add(data);

    readonly object _moveLock = new object();
    readonly List<PendingMove> _pending = new();
    readonly List<PendingMove> _drain = new();

    protected override void Awake()
    {
        base.Awake();
        CachePrefabs();
    }

#if UNITY_EDITOR
    void OnValidate() => CachePrefabs();
#endif

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

    public bool TryGet(ObjectKind kind, int id, out WorldObject obj) =>
        _objects.TryGetValue((kind, id), out obj);

    public void Despawn(ObjectKind kind, int id)
    {
        var key = (kind, id);
        if (!_objects.TryGetValue(key, out var obj) || obj == null) return;
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

    public void QueueMove(ObjectKind kind, int id, Vector3 pos, float rotation, sbyte moveType, float velX = 0f, float velZ = 0f, bool isSprinting = false, bool isRolling = false) =>
        Enqueue(new PendingMove { Kind = kind, Id = id, Pos = pos, Rotation = rotation, MoveType = moveType, VelocityX = velX, VelocityZ = velZ, IsSprinting = isSprinting, IsRolling = isRolling });


    void ApplyMove(in PendingMove m)
    {
        if (IsLocalPlayer(m.Kind, m.Id)) return;

        var key = (m.Kind, m.Id);
        if (!_objects.TryGetValue(key, out var obj))
        {
            obj = CreateWorldObject(m.Kind, m.Id, m.TypeId);
            obj.transform.SetPositionAndRotation(m.Pos, Quaternion.Euler(0f, m.Rotation, 0f));
            _objects.Add(key, obj);
        }

        obj.SetMoveTarget(m.Pos, m.Rotation, m.VelocityX, m.VelocityZ, m.IsSprinting, m.IsRolling);
    }

    static bool IsLocalPlayer(ObjectKind kind, int id)
    {
        if (kind != ObjectKind.Player) return false;
        var nm = NetworkManager.Instance;
        return nm != null && id == nm.MyPlayerId;
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

        foreach (var kv in _objects)
        {
            if (kv.Value != null)
                Destroy(kv.Value.gameObject);
        }
        _objects.Clear();
        _spawnedBoxes.Clear();
    }

    public ItemBox SpawnItemBox(int uid, int typeId, Vector3 pos, float rotation)
    {
        var obj = CreateWorldObject(ObjectKind.ItemBox, uid, typeId);
        obj.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotation, 0f));
        _objects[(ObjectKind.ItemBox, uid)] = obj;
        return obj.GetComponent<ItemBox>() ?? obj.gameObject.AddComponent<ItemBox>();
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
        return component;
    }

    static GameObject CreateFallback()
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        Destroy(go.GetComponent<Collider>());
        return go;
    }
}
