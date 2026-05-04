using System.Linq;

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
        public required List<int> ItemIds;
    }

    public void TempInit()
    {
        lock (_lock)
        {
            if (_items.Count > 0)
                return;

            SpawnBox(301, 0f, 0f, 4f, 0f, new[] { 101, 101, 101, 101, 101, 101, 101, 101, 102, 103 });
            SpawnBox(301, 0f, 0f, 8f, 0f, new[] { 102, 103 });
        }
    }

    public void SpawnBox(int typeId, float x, float y, float z, float rot, int[] itemIds)
    {
        int uid;
        lock (_lock)
        {
            uid = _nextUid++;
            _items[uid] = new Entry { TypeId = typeId, X = x, Y = y, Z = z, Rotation = rot, ItemIds = new List<int>(itemIds) };
        }

        PacketBuilder.Broadcast(SessionManager.Instance.Snapshot(), BuildSpawnPkt(uid, typeId, x, y, z, rot, itemIds), S_SpawnItemBoxNtf.Pack, PacketType.S_SpawnItemBoxNtf);
        LOG($"ItemBox 스폰: uid={uid}, pos=({x},{y},{z}), items=[{string.Join(",", itemIds)}]");
    }

    public void SyncTo(ClientSession session)
    {
        List<KeyValuePair<int, Entry>> copy;
        lock (_lock) { copy = _items.ToList(); }

        foreach (var kv in copy)
        {
            Entry e = kv.Value;
            PacketBuilder.Send(session, BuildSpawnPkt(kv.Key, e.TypeId, e.X, e.Y, e.Z, e.Rotation, e.ItemIds.ToArray()), S_SpawnItemBoxNtf.Pack, PacketType.S_SpawnItemBoxNtf);
        }
    }

    public bool AddItemToBox(int boxUid, int itemTypeId, int amount)
    {
        lock (_lock)
        {
            if (!_items.TryGetValue(boxUid, out var entry))
                return false;

            for (int i = 0; i < amount; i++)
                entry.ItemIds.Add(itemTypeId);
        }
        return true;
    }

    public bool TryTakeItem(int boxUid, int itemTypeId, int amount, out int takenAmount)
    {
        lock (_lock)
        {
            if (!_items.TryGetValue(boxUid, out var entry))
            {
                takenAmount = 0;
                return false;
            }

            int count = entry.ItemIds.Count(id => id == itemTypeId);
            takenAmount = Math.Min(count, amount);
            for (int i = 0; i < takenAmount; i++)
                entry.ItemIds.Remove(itemTypeId);

            return takenAmount > 0;
        }
    }

    public void Despawn(int uid)
    {
        lock (_lock) { _items.Remove(uid); }
        PacketBuilder.Broadcast(SessionManager.Instance.Snapshot(), new S_WorldItemDespawnNtfT { Uid = uid }, S_WorldItemDespawnNtf.Pack, PacketType.S_WorldItemDespawnNtf);
    }

    static S_SpawnItemBoxNtfT BuildSpawnPkt(int uid, int typeId, float x, float y, float z, float rot, int[] itemIds) =>
        new S_SpawnItemBoxNtfT
        {
            Uid      = uid,
            TypeId   = typeId,
            Pos      = new Vec3T { X = x, Y = y, Z = z },
            Rotation = rot,
            ItemIds  = itemIds.ToList(),
        };
}
