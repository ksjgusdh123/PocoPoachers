namespace Server;

public partial class PacketHandler
{
    public void OnC_GainItemReq(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_GainItemReq();
        if (session.Player is not { } player)
            return;

        bool isPlayer = pkt.IsPlayer;
        int boxUid = pkt.BoxUid;
        int itemTypeId = pkt.ItemUid;
        int amount = pkt.Amount;

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

        PacketSender.SChangeItemBox(session, isPlayer, boxUid, itemTypeId, amount);
        PacketSender.SSuccessGainItemNtf(session, boxUid, itemTypeId, amount);
    }
}
