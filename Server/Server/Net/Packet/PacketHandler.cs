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

    public void OnC_AddItemReq(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_AddItemReq();
        if (session.Player is not { } player)
            return;

        int itemId = pkt.ItemId;
        int amount = pkt.Amount;
        bool success = player.Inventory.AddItem(itemId, amount);
        PacketSender.SAddItemRes(session, success, itemId, success ? amount : 0);
    }

    public void OnC_RemoveItemReq(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_RemoveItemReq();
        if (session.Player is not { } player)
            return;

        int itemId = pkt.ItemId;
        int amount = pkt.Amount;
        bool success = player.Inventory.RemoveItem(itemId, amount);
        PacketSender.SRemoveItemRes(session, success, itemId, success ? amount : 0);
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
