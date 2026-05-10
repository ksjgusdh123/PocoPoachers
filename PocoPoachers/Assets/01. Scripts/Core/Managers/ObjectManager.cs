using System;
using System.Collections.Generic;
using UnityEngine;

struct PendingMove
{
    public ObjectKind Kind;
    public int Id;
    public Vector3 Pos;
    public float Rotation;
    public sbyte MoveType;
    public int TypeId;
}

public class ObjectManager : Singleton<ObjectManager>
{
    [Serializable]
    sealed class Entry
    {
        public ObjectKind Kind;
        public WorldObject Prefab;
    }

    [SerializeField] Entry[] _entries;

    readonly Dictionary<(ObjectKind kind, int id), WorldObject> _objects = new Dictionary<(ObjectKind, int), WorldObject>();
    readonly Dictionary<ObjectKind, WorldObject> _prefabs = new Dictionary<ObjectKind, WorldObject>();
    readonly List<H_ItemSpawnT> _spawnedBoxes = new();

    public IReadOnlyList<H_ItemSpawnT> SpawnedBoxes => _spawnedBoxes;
    public void RegisterSpawnedBox(H_ItemSpawnT data) => _spawnedBoxes.Add(data);

    readonly object _moveLock = new object();
    readonly List<PendingMove> _pending = new List<PendingMove>();
    readonly List<PendingMove> _drain = new List<PendingMove>();

    protected override void Awake()
    {
        base.Awake();
        CachePrefabs();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        CachePrefabs();
    }
#endif

    void CachePrefabs()
    {
        _prefabs.Clear();
        if (_entries == null)
            return;

        for (int i = 0; i < _entries.Length; i++)
        {
            Entry e = _entries[i];
            if (e == null || e.Prefab == null)
                continue;
            _prefabs[e.Kind] = e.Prefab;
        }
    }

    public bool TryGet(ObjectKind kind, int id, out WorldObject obj)
    {
        return _objects.TryGetValue((kind, id), out obj);
    }

    public void Despawn(ObjectKind kind, int id)
    {
        var key = (kind, id);
        if (!_objects.TryGetValue(key, out var obj) || obj == null)
            return;
        Destroy(obj.gameObject);
        _objects.Remove(key);
    }

    void Update()
    {
        _drain.Clear();
        lock (_moveLock)
        {
            if (_pending.Count == 0)
                return;
            _drain.AddRange(_pending);
            _pending.Clear();
        }

        for (int i = 0; i < _drain.Count; i++)
            ApplyMove(_drain[i]);
    }

    public void QueueMove(ObjectKind kind, int id, Vector3 pos, float rotation, sbyte moveType)
    {
        lock (_moveLock)
        {
            _pending.Add(new PendingMove
            {
                Kind = kind,
                Id = id,
                Pos = pos,
                Rotation = rotation,
                MoveType = moveType,
                TypeId = 0,
            });
        }
    }

    public void QueueWorldItemSpawn(int uid, int typeId, Vector3 pos, float rotation)
    {
        lock (_moveLock)
        {
            _pending.Add(new PendingMove
            {
                Kind = ObjectKind.WorldItem,
                Id = uid,
                Pos = pos,
                Rotation = rotation,
                MoveType = 0,
                TypeId = typeId,
            });
        }
    }

    void ApplyMove(in PendingMove m)
    {
        ObjectKind kind = m.Kind;
        int id = m.Id;
        Vector3 pos = m.Pos;
        float rotation = m.Rotation;

        if (IsLocalPlayer(kind, id))
            return;

        var key = (kind, id);
        if (!_objects.TryGetValue(key, out var obj))
        {
            obj = Spawn(kind, id, m.TypeId);
            obj.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotation, 0f));
            _objects.Add(key, obj);
        }

        obj.SetMoveTarget(pos, rotation);
    }

    static bool IsLocalPlayer(ObjectKind kind, int id)
    {
        if (kind != ObjectKind.Player)
            return false;

        var nm = NetworkManager.Instance;
        return nm != null && id == nm.MyPlayerId;
    }

    public List<PlayerInfoT> GetAllPlayerInfos(int excludeId = -1)
    {
        var list = new List<PlayerInfoT>();
        foreach (var kv in _objects)
        {
            if (kv.Key.kind != ObjectKind.Player) continue;
            if (kv.Key.id == excludeId) continue;
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
        var obj = Spawn(ObjectKind.ItemBox, uid, typeId);
        obj.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, rotation, 0f));
        _objects[(ObjectKind.ItemBox, uid)] = obj;
        var box = obj.GetComponent<ItemBox>();
        if (box == null)
        {
            box = obj.gameObject.AddComponent<ItemBox>();
        }
        return box;
    }

    WorldObject Spawn(ObjectKind kind, int id, int typeId = 0)
    {
        GameObject go;
        WorldObject obj;

        if (kind == ObjectKind.ItemBox && typeId > 0)
        {
            ItemData data = ItemTable.Instance.Get(typeId);
            GameObject prefabGo = data != null ? data.LoadPrefab() : null;
            if (prefabGo != null)
            {
                go = Instantiate(prefabGo);
                obj = go.GetComponent<WorldObject>();
                if (obj == null)
                    obj = go.AddComponent<WorldObject>();
                go.name = $"WorldItem_{id}_{typeId}";
                obj.Initialize(kind, id, typeId);
                return obj;
            }
        } 

        if (_prefabs.TryGetValue(kind, out WorldObject prefab) && prefab != null)
        {
            go = Instantiate(prefab.gameObject);
            obj = go.GetComponent<WorldObject>();
            if (obj == null)
                obj = go.AddComponent<WorldObject>();
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            var col = go.GetComponent<Collider>();
            if (col != null)
                Destroy(col);
            obj = go.AddComponent<WorldObject>();
        }

        go.name = $"{kind}_{id}";
        obj.Initialize(kind, id, typeId);
        return obj;
    }
}
