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
        int[] item_counts = pkt.GetItemCountArray();
        int[] item_uids = pkt.GetItemUidsArray();

        MainThreadDispatcher.Enqueue(() =>
        {
            var box = ObjectManager.Instance?.SpawnItemBox(uid, typeId, pos, rotation);
            box?.Initialize(item_ids, item_counts, item_uids);
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

    // 낙관적 업데이트 적용 후 결과 수신 — 실패 시만 롤백
    public static void OnH_ItemGainResult(FlatPacket root)
    {
        var pkt = root.TypeAsH_ItemGainResult();
        if (pkt.Success) return; // 이미 로컬에 적용됨

        var itemData = ItemTable.Instance.Get(pkt.ItemTypeId);
        if (itemData == null) return;

        var playerInv = FindLocalInventory();
        var om = ObjectManager.Instance;
        Inventory boxInv = null;
        if (om != null && om.TryGet(ObjectKind.ItemBox, pkt.BoxUid, out var boxObj))
            boxInv = boxObj.GetComponent<Inventory>();

        if (pkt.Amount > 0)
        {
            // 플레이어가 가져간 것 롤백: 플레이어에서 제거, 박스로 반환
            playerInv?.RemoveItemAtSlot(pkt.PlayerSlotIndex, itemData, pkt.Amount);
            boxInv?.AddItemAtSlot(pkt.BoxSlotIndex, itemData, pkt.Amount, pkt.ItemUid);
        }
        else
        {
            // 플레이어가 넣은 것 롤백: 박스에서 제거, 플레이어로 반환
            boxInv?.RemoveItemAtSlot(pkt.BoxSlotIndex, itemData, -pkt.Amount);
            playerInv?.AddItemAtSlot(pkt.PlayerSlotIndex, itemData, -pkt.Amount, pkt.ItemUid);
        }
    }

    public static void OnH_ItemBoxUpdate(FlatPacket root)
    {
        var pkt = root.TypeAsH_ItemBoxUpdate();

        var om = ObjectManager.Instance;
        if (om == null || !om.TryGet(ObjectKind.ItemBox, pkt.BoxUid, out var boxObj)) return;

        var boxInv = boxObj.GetComponent<Inventory>();
        var itemData = ItemTable.Instance.Get(pkt.ItemTypeId);
        if (boxInv == null || itemData == null) return;

        if (pkt.Amount > 0)
        {
            int slotIndex = pkt.SlotIndex >= 0 ? pkt.SlotIndex : boxInv.CanAddItem(itemData, pkt.Amount);
            if (slotIndex >= 0) boxInv.AddItemAtSlot(slotIndex, itemData, pkt.Amount, pkt.ItemUid);
        }
        else
        {
            int slotIndex = pkt.SlotIndex >= 0 ? pkt.SlotIndex : boxInv.FindItemSlotIndex(itemData);
            if (slotIndex >= 0) boxInv.RemoveItemAtSlot(slotIndex, itemData, -pkt.Amount);
        }
    }

    public static void OnH_ConsumeItemResult(FlatPacket root)
    {
        var pkt = root.TypeAsH_ConsumeItemResult();
        int myId = NetworkManager.Instance?.MyPlayerId ?? 0;
        if (pkt.PlayerId == myId) return;

        var itemData = ItemTable.Instance.Get(pkt.ItemId);
        if (itemData == null) return;

        MainThreadDispatcher.Enqueue(() =>
        {
            if (!ObjectManager.Instance.TryGet(ObjectKind.Player, pkt.PlayerId, out var worldObj)) return;
            if (worldObj.GetComponent<RemotePlayerStat>() is not RemotePlayerStat remote) return;
            remote.ApplyConsumableEffect(itemData);
        });
    }

    private static Inventory FindLocalInventory()
    {
        return Object.FindAnyObjectByType<PlayerController>()?.GetComponent<Inventory>();
    }
}
