namespace Server;

public partial class PacketHandler
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
}
