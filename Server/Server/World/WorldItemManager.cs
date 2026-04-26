using System.Linq;
using Google.FlatBuffers;

namespace Server;

public sealed class WorldItemManager
{
    public static WorldItemManager Instance { get; } = new();

    readonly object _lock = new();
    readonly Dictionary<int, Entry> _items = new();
    int _nextUid = 1;

    sealed class Entry
    {
        public int TypeId;
        public float X, Y, Z, Rotation;
    }

    public void TempInit()
    {
        lock (_lock)
        {
            if (_items.Count > 0)
                return;

            Add(101, 2f, 0f, 3f, 0f);
            Add(102, -1.5f, 0f, 2.5f, 30f);
            Add(103, 0f, 0f, 6f, 0f);
        }
    }

    void Add(int typeId, float x, float y, float z, float rot)
    {
        if (ItemTable.Get(typeId) == null)
        {
            LOG_W($"WorldItem: type_id {typeId} 없음, 스킵");
            return;
        }

        int uid = _nextUid++;
        _items[uid] = new Entry { TypeId = typeId, X = x, Y = y, Z = z, Rotation = rot };
        LOG($"WorldItem 등록: uid={uid}, type_id={typeId}");
    }

    public void SyncTo(ClientSession session)
    {
        List<KeyValuePair<int, Entry>> copy;
        lock (_lock)
        {
            copy = _items.ToList();
        }

        foreach (var kv in copy)
        {
            Entry e = kv.Value;
            SendSpawn(session, kv.Key, e.TypeId, e.X, e.Y, e.Z, e.Rotation);
        }
    }

    public void Spawn(int uid, int typeId, float x, float y, float z, float rot)
    {
        var fb = new FlatBufferBuilder(128);
        Offset<S_WorldItemSpawnNtf> bodyOff = BuildSpawn(fb, uid, typeId, x, y, z, rot);
        PacketBuilder.Broadcast(SessionManager.Instance.Snapshot(), fb, PacketType.S_WorldItemSpawnNtf, bodyOff.Value);
    }

    public void Despawn(int uid)
    {
        lock (_lock)
        {
            _items.Remove(uid);
        }

        var fb = new FlatBufferBuilder(64);
        Offset<S_WorldItemDespawnNtf> bodyOff = S_WorldItemDespawnNtf.CreateS_WorldItemDespawnNtf(fb, uid);
        PacketBuilder.Broadcast(SessionManager.Instance.Snapshot(), fb, PacketType.S_WorldItemDespawnNtf, bodyOff.Value);
    }

    static void SendSpawn(ClientSession session, int uid, int typeId, float x, float y, float z, float rot)
    {
        var fb = new FlatBufferBuilder(128);
        Offset<S_WorldItemSpawnNtf> bodyOff = BuildSpawn(fb, uid, typeId, x, y, z, rot);
        PacketBuilder.Send(session, fb, PacketType.S_WorldItemSpawnNtf, bodyOff.Value);
    }

    static Offset<S_WorldItemSpawnNtf> BuildSpawn(FlatBufferBuilder fb, int uid, int typeId, float x, float y, float z, float rot)
    {
        S_WorldItemSpawnNtf.StartS_WorldItemSpawnNtf(fb);
        S_WorldItemSpawnNtf.AddUid(fb, uid);
        S_WorldItemSpawnNtf.AddTypeId(fb, typeId);
        S_WorldItemSpawnNtf.AddPos(fb, Vec3.CreateVec3(fb, x, y, z));
        S_WorldItemSpawnNtf.AddRotation(fb, rot);
        return S_WorldItemSpawnNtf.EndS_WorldItemSpawnNtf(fb);
    }
}
