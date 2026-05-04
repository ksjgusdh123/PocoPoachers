namespace Server;

public partial class PacketHandler
{
    public void OnC_GainItemReq(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_GainItemReq();
        if (session.Player is not { } player)
            return;

        bool isPlayer        = pkt.IsPlayer;
        int  boxUid          = pkt.BoxUid;
        int  itemTypeId      = pkt.ItemUid;
        int  amount          = pkt.Amount;
        int  slotIndex       = pkt.SlotIndex;
        int  removedSlotIndex= pkt.RemovedSlotIndex;

        if (isPlayer)
        {
            if (!session.Player.Inventory.RemoveItem(itemTypeId, amount))
            {
                LOG_W($"GainItemReq 실패(플레이어→박스): boxUid={boxUid}, itemTypeId={itemTypeId}, amount={amount}");
                return;
            }
            WorldItemManager.Instance.AddItemToBox(boxUid, itemTypeId, amount);
            LOG($"PlayerToBox: PlayerId={session.PlayerId}, boxUid={boxUid}, itemTypeId={itemTypeId}, amount={amount}");
        }
        else
        {
            if (!WorldItemManager.Instance.TryTakeItem(boxUid, itemTypeId, amount, out int taken))
            {
                LOG_W($"GainItemReq 실패(박스→플레이어): boxUid={boxUid}, itemTypeId={itemTypeId}, amount={amount}");
                return;
            }
            amount = taken;
            session.Player.Inventory.AddItem(itemTypeId, amount);
            LOG($"BoxToPlayer: PlayerId={session.PlayerId}, boxUid={boxUid}, itemTypeId={itemTypeId}, taken={amount}");
        }

        int changeSlot = (isPlayer && removedSlotIndex != -1) ? slotIndex : removedSlotIndex;
        PacketBuilder.Broadcast(SessionManager.Instance.Snapshot(session), new S_ChangeItemBoxT
        {
            IsGain    = isPlayer,
            BoxUid    = boxUid,
            TypeId    = itemTypeId,
            Amount    = amount,
            SlotIndex = changeSlot,
        }, S_ChangeItemBox.Pack, PacketType.S_ChangeItemBox);
        LOG($"boxSlotIndex : {changeSlot}");

        PacketBuilder.Send(session, new S_SuccessGainItemNtfT
        {
            Uid              = boxUid,
            TypeId           = itemTypeId,
            Amount           = amount,
            SlotIndex        = slotIndex,
            RemovedSlotIndex = removedSlotIndex,
        }, S_SuccessGainItemNtf.Pack, PacketType.S_SuccessGainItemNtf);
    }

    public void OnC_ExchangeItemReq(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_ExchangeItemReq();
        if (session.Player is not { } player)
            return;

        int boxUid           = pkt.BoxUid;
        int playerItemId     = pkt.PlayerItemId;
        int playerItemAmount = pkt.PlayerItemAmount;
        int playerSlotIndex  = pkt.PlayerSlotIndex;
        int boxItemId        = pkt.BoxItemId;
        int boxItemAmount    = pkt.BoxItemAmount;
        int boxSlotIndex     = pkt.BoxSlotIndex;

        if (!WorldItemManager.Instance.TryTakeItem(boxUid, boxItemId, boxItemAmount, out int taken))
        {
            LOG_W($"GainItemReq 실패(박스→플레이어): boxUid={boxUid}, itemTypeId={boxItemId}, amount={boxItemAmount}");
            return;
        }
        boxItemAmount = taken;
        session.Player.Inventory.AddItem(boxItemId, boxItemAmount);

        if (!session.Player.Inventory.RemoveItem(playerItemId, playerItemAmount))
        {
            LOG_W($"GainItemReq 실패(플레이어→박스): boxUid={boxUid}, itemTypeId={playerItemId}, amount={playerItemAmount}");
            return;
        }
        WorldItemManager.Instance.AddItemToBox(boxUid, playerItemId, playerItemAmount);

        var others = SessionManager.Instance.Snapshot(session);

        // 교환으로 상자가 소실
        PacketBuilder.Broadcast(others, new S_ChangeItemBoxT
        {
            IsGain = false, BoxUid = boxUid, TypeId = boxItemId, Amount = boxItemAmount, SlotIndex = boxSlotIndex,
        }, S_ChangeItemBox.Pack, PacketType.S_ChangeItemBox);
        // 교환으로 상자가 획득
        PacketBuilder.Broadcast(others, new S_ChangeItemBoxT
        {
            IsGain = true, BoxUid = boxUid, TypeId = playerItemId, Amount = playerItemAmount, SlotIndex = boxSlotIndex,
        }, S_ChangeItemBox.Pack, PacketType.S_ChangeItemBox);

        PacketBuilder.Send(session, new S_ExchangeItemResultNtfT
        {
            IsSuccess            = true,
            BoxUid               = boxUid,
            PlayerGainItemId     = playerItemId,
            PlayerGainItemAmount = playerItemAmount,
            PlayerSlotIndex      = playerSlotIndex,
            BoxGainItemId        = boxItemId,
            BoxGainItemAmount    = boxItemAmount,
            BoxSlotIndex         = boxSlotIndex,
        }, S_ExchangeItemResultNtf.Pack, PacketType.S_ExchangeItemResultNtf);
    }
}
