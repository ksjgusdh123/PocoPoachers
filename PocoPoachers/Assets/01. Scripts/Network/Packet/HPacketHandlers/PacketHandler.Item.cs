using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnH_ItemSpawn(FlatPacket root)
    {
        var packet = root.TypeAsH_ItemSpawn();
        int uid = packet.Uid;
        int typeId = packet.TypeId;
        float x = packet.Pos?.X ?? 0f;
        float y = packet.Pos?.Y ?? 0f;
        float z = packet.Pos?.Z ?? 0f;
        Vector3 pos = new Vector3(x, y, z);
        float rotation = packet.Rotation;
        int[] item_ids = packet.GetItemIdsArray();
        int[] item_counts = packet.GetItemCountArray();
        int[] item_uids = packet.GetItemUidsArray();
        int[] item_slots = packet.GetItemSlotsArray();

        MainThreadDispatcher.Enqueue(() =>
        {
            var objectManager = ObjectManager.Instance;
            if (objectManager == null) return;

            // 씬에 미리 배치되어 이미 등록된 오브젝트(쉘터 창고 등)는 스폰 없이 내용물만 갱신
            if (objectManager.TryGet(ObjectKind.ItemBox, uid, out var existing) && existing != null)
            {
                FillInventory(existing.GetComponent<Inventory>(), item_ids, item_counts, item_uids, item_slots);
                return;
            }

            var box = objectManager.SpawnItemBox(uid, typeId, pos, rotation);
            box?.Initialize(item_ids, item_counts, item_uids);
        });
    }

    // 스냅샷 내용으로 인벤토리를 통째로 교체. item_slots가 있으면 호스트와 동일한 슬롯에 배치
    private static void FillInventory(Inventory inventory, int[] itemIds, int[] itemCounts, int[] itemUids, int[] itemSlots)
    {
        if (inventory == null || itemIds == null) return;

        inventory.Clear();
        for (int i = 0; i < itemIds.Length; i++)
        {
            var data = ItemTable.Instance.Get(itemIds[i]);
            if (data == null) continue;

            int count = (itemCounts != null && i < itemCounts.Length) ? itemCounts[i] : 1;
            int uid   = (itemUids  != null && i < itemUids.Length)  ? itemUids[i]  : 0;
            int slot  = (itemSlots != null && i < itemSlots.Length) ? itemSlots[i] : inventory.CanAddItem(data, count);
            if (slot >= 0)
                inventory.AddItemAtSlot(slot, data, count, uid);
        }
    }

    public static void OnH_ItemDespawn(FlatPacket root)
    {
        var packet = root.TypeAsH_ItemDespawn();
        int uid = packet.Uid;

        MainThreadDispatcher.Enqueue(() =>
        {
            ObjectManager.Instance?.Despawn(ObjectKind.WorldItem, uid);
        });
    }

    // H_ItemGainResult failure: roll back optimistic local apply
    public static void OnH_ItemGainResult(FlatPacket root)
    {
        var packet = root.TypeAsH_ItemGainResult();
        if (packet.Success) return; // already applied locally

        var itemData = ItemTable.Instance.Get(packet.ItemTypeId);
        if (itemData == null) return;

        var playerInv = FindLocalInventory();
        var objectManager = ObjectManager.Instance;
        Inventory boxInventory = null;
        if (objectManager != null && objectManager.TryGet(ObjectKind.ItemBox, packet.BoxUid, out var boxObj))
            boxInventory = boxObj.GetComponent<Inventory>();

        if (packet.Amount > 0)
        {
            // rollback take: remove from player, return to box
            playerInv?.RemoveItemAtSlot(packet.PlayerSlotIndex, itemData, packet.Amount);
            boxInventory?.AddItemAtSlot(packet.BoxSlotIndex, itemData, packet.Amount, packet.ItemUid);
        }
        else
        {
            // rollback place: remove from box, return to player
            boxInventory?.RemoveItemAtSlot(packet.BoxSlotIndex, itemData, -packet.Amount);
            playerInv?.AddItemAtSlot(packet.PlayerSlotIndex, itemData, -packet.Amount, packet.ItemUid);
        }
    }

    public static void OnH_ItemExchangeResult(FlatPacket root)
    {
        var packet = root.TypeAsH_ItemExchangeResult();
        if (packet.Success) return;

        MainThreadDispatcher.Enqueue(() =>
        {
            SlotInteractionManager.GetInstance()?.RollbackExchange(
                packet.BoxUid,
                packet.PlayerSlotIndex,
                packet.PlayerItemId,
                packet.PlayerItemAmount,
                packet.PlayerItemUid,
                packet.BoxSlotIndex,
                packet.BoxItemId,
                packet.BoxItemAmount,
                packet.BoxItemUid);
        });
    }

    public static void OnH_ItemBoxUpdate(FlatPacket root)
    {
        var packet = root.TypeAsH_ItemBoxUpdate();

        var objectManager = ObjectManager.Instance;
        if (objectManager == null || !objectManager.TryGet(ObjectKind.ItemBox, packet.BoxUid, out var boxObj)) return;

        var boxInventory = boxObj.GetComponent<Inventory>();
        var itemData = ItemTable.Instance.Get(packet.ItemTypeId);
        if (boxInventory == null || itemData == null) return;

        if (packet.Amount > 0)
        {
            int slotIndex = packet.SlotIndex >= 0 ? packet.SlotIndex : boxInventory.CanAddItem(itemData, packet.Amount);
            if (slotIndex >= 0) boxInventory.AddItemAtSlot(slotIndex, itemData, packet.Amount, packet.ItemUid);
        }
        else
        {
            int slotIndex = packet.SlotIndex >= 0 ? packet.SlotIndex : boxInventory.FindItemSlotIndex(itemData);
            if (slotIndex >= 0) boxInventory.RemoveItemAtSlot(slotIndex, itemData, -packet.Amount);
        }
    }

    public static void OnH_ConsumeItemResult(FlatPacket root)
    {
        var packet = root.TypeAsH_ConsumeItemResult();
        int myId = NetworkManager.Instance?.MyPlayerId ?? 0;
        if (packet.PlayerId == myId) return;

        var itemData = ItemTable.Instance.Get(packet.ItemId);
        if (itemData == null) return;

        MainThreadDispatcher.Enqueue(() =>
        {
            if (!ObjectManager.Instance.TryGet(ObjectKind.Player, packet.PlayerId, out var worldObj)) return;
            if (worldObj.GetComponent<RemotePlayerStat>() is not RemotePlayerStat remote) return;
            remote.ApplyConsumableEffect(itemData);
        });
    }

    private static Inventory FindLocalInventory()
    {
        return UnityEngine.Object.FindAnyObjectByType<PlayerController>()?.GetComponent<Inventory>();
    }
}
