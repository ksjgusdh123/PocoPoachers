using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnG_ItemGain(FlatPacket root)
    {
        var pkt = root.TypeAsG_ItemGain();
        if (!RoomManager.IsHost) return;

        int requesterId  = RoomManager.LastGuestId;
        bool success     = false;
        int changedSlot  = -1;

        var om       = ObjectManager.Instance;
        var itemData = ItemTable.Instance.Get(pkt.ItemTypeId);

        if (om != null && itemData != null && om.TryGet(ObjectKind.ItemBox, pkt.BoxUid, out var boxObj))
        {
            var boxInv = boxObj.GetComponent<Inventory>();
            if (boxInv != null)
            {
                if (pkt.IsPlayerGained)
                {
                    // 플레이어가 박스에서 가져감 → 박스에서 제거
                    for (int i = 0; i < boxInv.Slots.Count; i++)
                    {
                        if (!boxInv.Slots[i].IsEmpty && boxInv.Slots[i].ItemData?.Id == pkt.ItemTypeId)
                        {
                            changedSlot = i;
                            break;
                        }
                    }
                    if (changedSlot >= 0)
                    {
                        boxInv.RemoveItemAtSlot(changedSlot, itemData, pkt.Amount);
                        success = true;
                    }
                }
                else
                {
                    // 플레이어가 박스에 넣음 → 박스에 추가
                    changedSlot = pkt.SlotIndex;
                    success = boxInv.AddItemAtSlot(changedSlot, itemData, pkt.Amount);
                }
            }
        }

        // 요청자에게 결과 응답
        PacketBuilder.SendToGuest(requesterId, new H_ItemGainResultT
        {
            Success          = success,
            BoxUid           = pkt.BoxUid,
            ItemTypeId       = pkt.ItemTypeId,
            Amount           = pkt.Amount,
            SlotIndex        = pkt.SlotIndex,
            RemovedSlotIndex = changedSlot,
        }, H_ItemGainResult.Pack, PacketType.H_ItemGainResult);

        // 나머지 클라이언트에게 박스 변경 브로드캐스트
        // IsPlayerGained=true(제거)면 음수, false(추가)면 양수
        if (success)
        {
            int updateAmount = pkt.IsPlayerGained ? -pkt.Amount : pkt.Amount;
            PacketBuilder.BroadcastToGuests(requesterId, new H_ItemBoxUpdateT
            {
                BoxUid     = pkt.BoxUid,
                ItemTypeId = pkt.ItemTypeId,
                Amount     = updateAmount,
                SlotIndex  = changedSlot,
            }, H_ItemBoxUpdate.Pack, PacketType.H_ItemBoxUpdate);
        }
    }

    public static void OnG_ItemExchange(FlatPacket root)
    {
        var pkt = root.TypeAsG_ItemExchange();
        if (!RoomManager.IsHost) return;

        int requesterId = RoomManager.LastGuestId;
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

        PacketBuilder.SendToGuest(requesterId, new H_ItemExchangeResultT
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

