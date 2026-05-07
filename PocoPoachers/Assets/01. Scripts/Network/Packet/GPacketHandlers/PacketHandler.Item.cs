using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnG_ItemGain(FlatPacket root)
    {
        var pkt = root.TypeAsG_ItemGain();
        var p2p = P2PManager.Instance;
        if (p2p == null || !p2p.IsHost) return;

        int requesterId     = p2p.LastSenderPlayerId;
        bool success        = false;
        int removedSlotIdx  = -1;

        var om = ObjectManager.Instance;
        if (om != null && om.TryGet(ObjectKind.ItemBox, pkt.BoxUid, out var boxObj))
        {
            var boxInv   = boxObj.GetComponent<Inventory>();
            var itemData = ItemTable.Instance.Get(pkt.ItemTypeId);

            if (boxInv != null && itemData != null && boxInv.HasItem(itemData, pkt.Amount))
            {
                // 박스에서 해당 아이템이 있는 슬롯 탐색
                for (int i = 0; i < boxInv.Slots.Count; i++)
                {
                    var slot = boxInv.Slots[i];
                    if (!slot.IsEmpty && slot.ItemData?.Id == pkt.ItemTypeId)
                    {
                        removedSlotIdx = i;
                        break;
                    }
                }

                if (removedSlotIdx >= 0)
                {
                    boxInv.RemoveItemAtSlot(removedSlotIdx, itemData, pkt.Amount);
                    success = true;
                }
            }
        }

        p2p.SendTo(requesterId, new H_ItemGainResultT
        {
            Success         = success,
            BoxUid          = pkt.BoxUid,
            ItemTypeId      = pkt.ItemTypeId,
            Amount          = pkt.Amount,
            SlotIndex       = pkt.SlotIndex,
            RemovedSlotIndex = removedSlotIdx,
        }, H_ItemGainResult.Pack, PacketType.H_ItemGainResult);
    }

    public static void OnG_ItemExchange(FlatPacket root)
    {
        var pkt = root.TypeAsG_ItemExchange();
        var p2p = P2PManager.Instance;
        if (p2p == null || !p2p.IsHost) return;

        int requesterId = p2p.LastSenderPlayerId;
        bool success    = false;

        var om = ObjectManager.Instance;
        if (om != null && om.TryGet(ObjectKind.ItemBox, pkt.BoxUid, out var boxObj))
        {
            var boxInv       = boxObj.GetComponent<Inventory>();
            var boxItemData  = ItemTable.Instance.Get(pkt.BoxItemId);

            if (boxInv != null && boxItemData != null
                && pkt.BoxSlotIndex >= 0 && pkt.BoxSlotIndex < boxInv.Slots.Count)
            {
                var boxSlot = boxInv.Slots[pkt.BoxSlotIndex];
                if (!boxSlot.IsEmpty
                    && boxSlot.ItemData?.Id == pkt.BoxItemId
                    && boxSlot.Amount >= pkt.BoxItemAmount)
                {
                    boxInv.RemoveItemAtSlot(pkt.BoxSlotIndex, boxItemData, pkt.BoxItemAmount);

                    // 플레이어 아이템을 박스 슬롯에 추가
                    var playerItemData = ItemTable.Instance.Get(pkt.PlayerItemId);
                    if (playerItemData != null && pkt.PlayerItemAmount > 0)
                        boxInv.AddItemAtSlot(pkt.BoxSlotIndex, playerItemData, pkt.PlayerItemAmount);

                    success = true;
                }
            }
        }

        p2p.SendTo(requesterId, new H_ItemExchangeResultT
        {
            Success           = success,
            BoxUid            = pkt.BoxUid,
            PlayerItemId      = pkt.PlayerItemId,
            PlayerItemAmount  = pkt.PlayerItemAmount,
            PlayerSlotIndex   = pkt.PlayerSlotIndex,
            BoxItemId         = pkt.BoxItemId,
            BoxItemAmount     = pkt.BoxItemAmount,
            BoxSlotIndex      = pkt.BoxSlotIndex,
        }, H_ItemExchangeResult.Pack, PacketType.H_ItemExchangeResult);
    }

    public static void OnG_ConsumeItem(FlatPacket root)
    {
        //var pkt = root.TypeAsC_ConsumeItem();
        //if (session.Player is not { } player)
        //    return;

        //int itemTypeId = pkt.ItemId;
        //int amount = pkt.Amount;
        //int slotIndex = pkt.InventoryIndex;

        //if (!session.Player.Inventory.RemoveItem(itemTypeId, amount))
        //{
        //    LOG_W($"아이템 사용 실패(ID : {itemTypeId}, 수량 : {amount}");
        //    return;
        //}
        //PacketBuilder.Send(session, new S_ConsumeItemNtfT
        //{
        //    Amount = amount,
        //    InventoryIndex = slotIndex,
        //    IsSuccess = true,
        //    ItemId = itemTypeId,
        //}, S_ConsumeItemNtf.Pack, PacketType.S_ConsumeItemNtf);
    }
}
