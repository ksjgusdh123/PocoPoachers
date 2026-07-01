using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnG_ItemGain(FlatPacket root)
    {
        var pkt = root.TypeAsG_ItemGain();
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryResolveGuestSender(0, allowAutoRegister: false, out int requesterId))
            return;

        bool success = false;

        var om = ObjectManager.Instance;
        var itemData = ItemTable.Instance.Get(pkt.ItemTypeId);

        if (om != null && itemData != null && om.TryGet(ObjectKind.ItemBox, pkt.BoxUid, out var boxObj))
        {
            var boxInv = boxObj.GetComponent<Inventory>();
            if (boxInv != null && pkt.Amount > 0)
            {
                if (pkt.IsPlayerGained)
                {
                    if (pkt.RemovedSlotIndex >= 0 && pkt.RemovedSlotIndex < boxInv.Slots.Count)
                    {
                        var slot = boxInv.Slots[pkt.RemovedSlotIndex];
                        if (!slot.IsEmpty
                            && slot.ItemData?.Id == pkt.ItemTypeId
                            && slot.Amount >= pkt.Amount)
                        {
                            boxInv.RemoveItemAtSlot(pkt.RemovedSlotIndex, itemData, pkt.Amount);
                            success = true;
                        }
                    }
                }
                else
                {
                    success = boxInv.AddItemAtSlot(pkt.AddedSlotIndex, itemData, pkt.Amount, pkt.ItemUid);
                }
            }
        }

        int playerSlotIndex = pkt.IsPlayerGained ? pkt.AddedSlotIndex : pkt.RemovedSlotIndex;
        int boxSlotIndex    = pkt.IsPlayerGained ? pkt.RemovedSlotIndex : pkt.AddedSlotIndex;

        PacketBuilder.SendToGuest(requesterId, new H_ItemGainResultT
        {
            Success         = success,
            BoxUid          = pkt.BoxUid,
            ItemTypeId      = pkt.ItemTypeId,
            ItemUid         = pkt.ItemUid,
            Amount          = pkt.IsPlayerGained ? pkt.Amount : -pkt.Amount,
            PlayerSlotIndex = playerSlotIndex,
            BoxSlotIndex    = boxSlotIndex,
        }, H_ItemGainResult.Pack, PacketType.H_ItemGainResult);

        if (success)
        {
            int updateAmount = pkt.IsPlayerGained ? -pkt.Amount : pkt.Amount;
            PacketBuilder.BroadcastToGuests(new H_ItemBoxUpdateT
            {
                BoxUid     = pkt.BoxUid,
                ItemTypeId = pkt.ItemTypeId,
                ItemUid    = pkt.ItemUid,
                Amount     = updateAmount,
                SlotIndex  = boxSlotIndex,
            }, H_ItemBoxUpdate.Pack, PacketType.H_ItemBoxUpdate);
        }
    }

    public static void OnG_ItemExchange(FlatPacket root)
    {
        var pkt = root.TypeAsG_ItemExchange();
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryResolveGuestSender(0, allowAutoRegister: false, out _))
            return;

        var om = ObjectManager.Instance;
        if (om != null && om.TryGet(ObjectKind.ItemBox, pkt.BoxUid, out var boxObj))
        {
            var boxInv = boxObj.GetComponent<Inventory>();
            var boxItemData = ItemTable.Instance.Get(pkt.BoxItemId);

            if (boxInv != null && boxItemData != null
                && pkt.BoxSlotIndex >= 0 && pkt.BoxSlotIndex < boxInv.Slots.Count)
            {
                var boxSlot = boxInv.Slots[pkt.BoxSlotIndex];
                if (!boxSlot.IsEmpty
                    && boxSlot.ItemData?.Id == pkt.BoxItemId
                    && boxSlot.Amount >= pkt.BoxItemAmount)
                {
                    boxInv.RemoveItemAtSlot(pkt.BoxSlotIndex, boxItemData, pkt.BoxItemAmount);

                    var playerItemData = ItemTable.Instance.Get(pkt.PlayerItemId);
                    if (playerItemData != null && pkt.PlayerItemAmount > 0)
                        boxInv.AddItemAtSlot(pkt.BoxSlotIndex, playerItemData, pkt.PlayerItemAmount);

                    RoomSync.ItemBoxUpdate(pkt.BoxUid, pkt.BoxItemId,    -pkt.BoxItemAmount,    pkt.BoxSlotIndex);
                    RoomSync.ItemBoxUpdate(pkt.BoxUid, pkt.PlayerItemId,  pkt.PlayerItemAmount,  pkt.BoxSlotIndex);
                }
            }
        }
    }

    public static void OnG_ConsumeItem(FlatPacket root)
    {
        var pkt = root.TypeAsG_ConsumeItem();
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryResolveGuestSender(0, allowAutoRegister: false, out int senderId))
            return;

        PacketBuilder.BroadcastToGuests(new H_ConsumeItemResultT
        {
            PlayerId = senderId,
            ItemId   = pkt.ItemId,
        }, H_ConsumeItemResult.Pack, PacketType.H_ConsumeItemResult);
    }
}
