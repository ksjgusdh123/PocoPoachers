using Google.FlatBuffers;
using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnS_WorldItemDespawnNtf(FlatPacket root)
    {
        var pkt = root.TypeAsS_WorldItemDespawnNtf();
        int uid = pkt.Uid;

        MainThreadDispatcher.Enqueue(() =>
        {
            ObjectManager.Instance?.Despawn(ObjectKind.WorldItem, uid);
        });
    }

    public static void OnS_SpawnItemBoxNtf(FlatPacket root)
    {
        var pkt = root.TypeAsS_SpawnItemBoxNtf();
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

    public static void OnS_ChangeItemBox(FlatPacket root)
    {
        var pkt = root.TypeAsS_ChangeItemBox();
        bool isGain = pkt.IsGain;
        int boxUid = pkt.BoxUid;
        int typeId = pkt.TypeId;
        int amount = pkt.Amount;

        MainThreadDispatcher.Enqueue(() =>
        {
            ItemData data = ItemTable.Instance.Get(typeId);
            if (data == null) return;

            if (!ObjectManager.Instance.TryGet(ObjectKind.ItemBox, boxUid, out var worldObj)) return;
            if (isGain) worldObj.GetComponent<Inventory>()?.AddItem(data, amount);
            else worldObj.GetComponent<Inventory>()?.RemoveItem(data, amount);
        });
    }

    public static void OnS_SuccessGainItemNtf(FlatPacket root)
    {
        var pkt = root.TypeAsS_SuccessGainItemNtf();
        var item = ItemTable.Instance.Get(pkt.TypeId);
        int amount = pkt.Amount;

        MainThreadDispatcher.Enqueue(() =>
        {
            GameManager.GetInstance().GainedInventory.AddItem(item, amount);
            GameManager.GetInstance().GiveInventory.RemoveItem(item, amount);
        });
    }
}
