using Unity.AppUI.UI;
using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnG_ItemGain(FlatPacket root)
    {
        var pkt = root.TypeAsG_ItemGain();
        if (!RoomManager.IsHost) return;

        int requesterId = RoomManager.LastGuestId;
        bool success = false;

        var om = ObjectManager.Instance;
        var itemData = ItemTable.Instance.Get(pkt.ItemTypeId);

        if (om != null && itemData != null && om.TryGet(ObjectKind.ItemBox, pkt.BoxUid, out var boxObj))
        {
            var boxInv = boxObj.GetComponent<Inventory>();
            if (boxInv != null)
            {
                if (pkt.IsPlayerGained)
                {
                    // 박스 → 플레이어: 박스에서 제거
                    boxInv.RemoveItemAtSlot(pkt.RemovedSlotIndex, itemData, pkt.Amount);
                    success = true;
                }
                else
                {
                    // 플레이어 → 박스: 클라이언트가 미리 확정한 슬롯에 추가
                    success = boxInv.AddItemAtSlot(pkt.AddedSlotIndex, itemData, pkt.Amount);
                }
            }
        }

        // 플레이어가 받는 경우 AddedSlotIndex를 그대로 전달, 주는 경우는 -1
        int playerSlotIndex = pkt.IsPlayerGained ? pkt.AddedSlotIndex : -1;

        PacketBuilder.SendToGuest(requesterId, new H_ItemGainResultT
        {
            Success         = success,
            BoxUid          = pkt.BoxUid,
            ItemTypeId      = pkt.ItemTypeId,
            Amount          = pkt.IsPlayerGained ? pkt.Amount : -pkt.Amount,
            PlayerSlotIndex = playerSlotIndex,
        }, H_ItemGainResult.Pack, PacketType.H_ItemGainResult);

        if (success)
        {
            int updateAmount = pkt.IsPlayerGained ? -pkt.Amount : pkt.Amount;
            int boxSlotIndex = pkt.IsPlayerGained ? pkt.RemovedSlotIndex : pkt.AddedSlotIndex;
            PacketBuilder.BroadcastToGuests(new H_ItemBoxUpdateT
            {
                BoxUid     = pkt.BoxUid,
                ItemTypeId = pkt.ItemTypeId,
                Amount     = updateAmount,
                SlotIndex  = boxSlotIndex,
            }, H_ItemBoxUpdate.Pack, PacketType.H_ItemBoxUpdate);
        }
    }

    public static void OnG_ItemExchange(FlatPacket root)
    {
        var pkt = root.TypeAsG_ItemExchange();
        if (!RoomManager.IsHost) return;

        int requesterId = RoomManager.LastGuestId;
        bool success = false;

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
            Success = success,
            BoxUid = pkt.BoxUid,
            PlayerItemId = pkt.PlayerItemId,
            PlayerItemAmount = pkt.PlayerItemAmount,
            PlayerSlotIndex = pkt.PlayerSlotIndex,
            BoxItemId = pkt.BoxItemId,
            BoxItemAmount = pkt.BoxItemAmount,
            BoxSlotIndex = pkt.BoxSlotIndex,
        }, H_ItemExchangeResult.Pack, PacketType.H_ItemExchangeResult);

        // except
        PacketBuilder.BroadcastToGuests(new H_ItemBoxUpdateT
        {
            BoxUid = pkt.BoxUid,
            ItemTypeId = pkt.BoxItemId,
            Amount = -pkt.BoxItemAmount,
            SlotIndex = pkt.BoxSlotIndex,
        }, H_ItemBoxUpdate.Pack, PacketType.H_ItemBoxUpdate);

        PacketBuilder.BroadcastToGuests(new H_ItemBoxUpdateT
        {
            BoxUid = pkt.BoxUid,
            ItemTypeId = pkt.PlayerItemId,
            Amount = pkt.PlayerItemAmount,
            SlotIndex = pkt.BoxSlotIndex,
        }, H_ItemBoxUpdate.Pack, PacketType.H_ItemBoxUpdate);
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

