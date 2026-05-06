using Google.FlatBuffers;
using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnH_ItemSpawn(FlatPacket root)
    {
        var pkt = root.TypeAsH_ItemSpawn();
        int uid = pkt.Uid;
        int typeId = pkt.TypeId;
        float x = pkt.Pos?.X ?? 0f;
        float y = pkt.Pos?.Y ?? 0f;
        float z = pkt.Pos?.Z ?? 0f;
        Vector3 pos = new Vector3(x, y, z);
        float rotation = pkt.Rotation;
        int[] item_ids = pkt.GetItemIdsArray();

        MainThreadDispatcher.Enqueue(() =>
        {
            var box = ObjectManager.Instance?.SpawnItemBox(uid, typeId, pos, rotation);
            box?.Initialize(item_ids);
        });
    }

    public static void OnH_ItemDespawn(FlatPacket root)
    {
        var pkt = root.TypeAsH_ItemDespawn();
        int uid = pkt.Uid;

        MainThreadDispatcher.Enqueue(() =>
        {
            ObjectManager.Instance?.Despawn(ObjectKind.WorldItem, uid);
        });
    }

    public static void OnG_ItemGain(FlatPacket root)
    {
        // TODO
    }

    public static void OnH_ItemGainResult(FlatPacket root)
    {
        // TODO
    }

    public static void OnH_InventoryUpdate(FlatPacket root)
    {
        // TODO
    }

    public static void OnG_ItemExchange(FlatPacket root)
    {
        // TODO
    }

    public static void OnH_ItemExchangeResult(FlatPacket root)
    {
        // TODO
    }
}
