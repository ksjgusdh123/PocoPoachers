using System;
using System.Collections.Generic;
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

        PacketSender.SSpawnItemBoxNtfBroadcast(uid, typeId, x, y, z, rot, itemIds.ToArray());
        LOG($"ItemBox 스폰: uid={uid}, pos=({x},{y},{z}), items=[{string.Join(",", itemIds)}]");
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
            PacketSender.SSpawnItemBoxNtfBroadcast(kv.Key, e.TypeId, e.X, e.Y, e.Z, e.Rotation, e.ItemIds.ToArray());
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
        lock (_lock)
        {
            _items.Remove(uid);
        }

        PacketSender.SWorldItemDespawnNtfBroadcast(uid);
    }
}
