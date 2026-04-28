namespace Server;

public class PacketHandler
{
    public void OnC_LoginReq(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_LoginReq();
        string userName = pkt.Username ?? string.Empty;
        bool success = !string.IsNullOrWhiteSpace(userName);

        if (success)
        {
            int playerId = SessionManager.Instance.GenerateId();
            session.Player = PlayerManager.Instance.CreatePlayer(playerId, userName);
            SessionManager.Instance.Add(session);
        }

        PacketSender.SLoginRes(
            session,
            success,
            success ? session.PlayerId : 0,
            success ? session.UserName : string.Empty,
            success ? session.Player!.Stat.Level : 1);

        if (success)
        {
            var snapshot = session.Player!.Inventory.GetSnapshot();
            PacketSender.SInventoryNtf(session, snapshot);
            WorldItemManager.Instance.SyncTo(session);
        }
    }

    public void OnC_MoveReq(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_MoveReq();
        if (session.Player is not { } player)
            return;

        var pos = pkt.Pos;
        if (!pos.HasValue)
            return;

        float x = pos.Value.X;
        float y = pos.Value.Y;
        float z = pos.Value.Z;
        float rotation = pkt.Rotation;
        sbyte moveType = pkt.MoveType;

        PacketSender.SMoveNtfBroadcast(session, player.PlayerId, x, y, z, rotation, moveType);
    }

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

    public void OnC_ShootReq(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_ShootReq();
        if (session.Player is not { } player)
            return;

        if (!pkt.Origin.HasValue || !pkt.Direction.HasValue)
            return;

        PacketSender.SShootNtfBroadcast(
            session,
            player.PlayerId,
            pkt.Origin.Value.X, pkt.Origin.Value.Y, pkt.Origin.Value.Z,
            pkt.Direction.Value.X, pkt.Direction.Value.Y, pkt.Direction.Value.Z,
            pkt.BulletSpeed,
            pkt.Damage,
            pkt.MaxRange);
    }
}
