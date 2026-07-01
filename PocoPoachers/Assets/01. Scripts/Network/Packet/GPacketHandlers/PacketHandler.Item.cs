using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnG_ItemGain(FlatPacket root)
    {
        var packet = root.TypeAsG_ItemGain();
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId))
            return;

        bool success = false;

        var objectManager = ObjectManager.Instance;
        var itemData = ItemTable.Instance.Get(packet.ItemTypeId);

        if (objectManager != null && itemData != null && objectManager.TryGet(ObjectKind.ItemBox, packet.BoxUid, out var boxObj))
        {
            var boxInventory = boxObj.GetComponent<Inventory>();
            if (boxInventory != null && packet.Amount > 0)
            {
                bool takeFromBox = packet.IsPlayerGained;
                if (takeFromBox)
                {
                    if (packet.RemovedSlotIndex >= 0 && packet.RemovedSlotIndex < boxInventory.Slots.Count)
                    {
                        var slot = boxInventory.Slots[packet.RemovedSlotIndex];
                        if (!slot.IsEmpty
                            && slot.ItemData?.Id == packet.ItemTypeId
                            && slot.Amount >= packet.Amount)
                        {
                            boxInventory.RemoveItemAtSlot(packet.RemovedSlotIndex, itemData, packet.Amount);
                            success = true;
                        }
                    }
                }
                else
                {
                    success = boxInventory.AddItemAtSlot(packet.AddedSlotIndex, itemData, packet.Amount, packet.ItemUid);
                }
            }
        }

        bool takeFromBoxResult = packet.IsPlayerGained;
        int playerSlot = takeFromBoxResult ? packet.AddedSlotIndex : packet.RemovedSlotIndex;
        int boxSlot    = takeFromBoxResult ? packet.RemovedSlotIndex : packet.AddedSlotIndex;

        PacketBuilder.SendReliableToGuest(guestId, new H_ItemGainResultT
        {
            Success         = success,
            BoxUid          = packet.BoxUid,
            ItemTypeId      = packet.ItemTypeId,
            ItemUid         = packet.ItemUid,
            Amount          = takeFromBoxResult ? packet.Amount : -packet.Amount,
            PlayerSlotIndex = playerSlot,
            BoxSlotIndex    = boxSlot,
        }, H_ItemGainResult.Pack, PacketType.H_ItemGainResult);

        if (success)
        {
            GuestInventoryTracker.ApplyGain(guestId, takeFromBoxResult, playerSlot, packet.ItemTypeId, packet.Amount, packet.ItemUid);

            int boxDelta = takeFromBoxResult ? -packet.Amount : packet.Amount;
            PacketBuilder.BroadcastToGuests(new H_ItemBoxUpdateT
            {
                BoxUid     = packet.BoxUid,
                ItemTypeId = packet.ItemTypeId,
                ItemUid    = packet.ItemUid,
                Amount     = boxDelta,
                SlotIndex  = boxSlot,
            }, H_ItemBoxUpdate.Pack, PacketType.H_ItemBoxUpdate);
        }
    }

    public static void OnG_ItemExchange(FlatPacket root)
    {
        var packet = root.TypeAsG_ItemExchange();
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId))
            return;

        bool success = false;
        var objectManager = ObjectManager.Instance;
        var boxItemData = ItemTable.Instance.Get(packet.BoxItemId);
        var playerItemData = ItemTable.Instance.Get(packet.PlayerItemId);

        if (packet.PlayerItemAmount <= 0 || GuestInventoryTracker.HasInSlot(guestId, packet.PlayerSlotIndex, packet.PlayerItemId, packet.PlayerItemAmount))
        {
            if (objectManager != null && objectManager.TryGet(ObjectKind.ItemBox, packet.BoxUid, out var boxObj))
            {
                var boxInventory = boxObj.GetComponent<Inventory>();
                if (boxInventory != null
                    && packet.BoxSlotIndex >= 0 && packet.BoxSlotIndex < boxInventory.Slots.Count
                    && boxItemData != null)
                {
                    var boxSlot = boxInventory.Slots[packet.BoxSlotIndex];
                    bool boxOk = packet.BoxItemAmount <= 0
                        || (!boxSlot.IsEmpty
                            && boxSlot.ItemData?.Id == packet.BoxItemId
                            && boxSlot.Amount >= packet.BoxItemAmount);

                    if (boxOk)
                    {
                        if (packet.BoxItemAmount > 0)
                            boxInventory.RemoveItemAtSlot(packet.BoxSlotIndex, boxItemData, packet.BoxItemAmount);

                        if (playerItemData != null && packet.PlayerItemAmount > 0)
                            boxInventory.AddItemAtSlot(packet.BoxSlotIndex, playerItemData, packet.PlayerItemAmount);

                        success = true;
                    }
                }
            }
        }

        PacketBuilder.SendReliableToGuest(guestId, new H_ItemExchangeResultT
        {
            Success            = success,
            BoxUid             = packet.BoxUid,
            PlayerItemId       = packet.PlayerItemId,
            PlayerItemAmount   = packet.PlayerItemAmount,
            PlayerSlotIndex    = packet.PlayerSlotIndex,
            BoxItemId          = packet.BoxItemId,
            BoxItemAmount      = packet.BoxItemAmount,
            BoxSlotIndex       = packet.BoxSlotIndex,
        }, H_ItemExchangeResult.Pack, PacketType.H_ItemExchangeResult);

        if (!success) return;

        GuestInventoryTracker.SetSlot(guestId, packet.PlayerSlotIndex, packet.BoxItemId, packet.BoxItemAmount);

        if (packet.BoxItemAmount > 0)
            RoomSync.ItemBoxUpdate(packet.BoxUid, packet.BoxItemId, -packet.BoxItemAmount, packet.BoxSlotIndex);
        if (packet.PlayerItemAmount > 0)
            RoomSync.ItemBoxUpdate(packet.BoxUid, packet.PlayerItemId, packet.PlayerItemAmount, packet.BoxSlotIndex);
    }

    public static void OnG_ConsumeItem(FlatPacket root)
    {
        var packet = root.TypeAsG_ConsumeItem();
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId))
            return;

        var itemData = ItemTable.Instance.Get(packet.ItemId);
        if (itemData == null || !ItemUseSystem.CanUse(itemData)) return;
        if (packet.Amount <= 0 || packet.Amount > 99) return;

        PacketBuilder.BroadcastToGuests(new H_ConsumeItemResultT
        {
            PlayerId = guestId,
            ItemId   = packet.ItemId,
        }, H_ConsumeItemResult.Pack, PacketType.H_ConsumeItemResult);
    }
}
