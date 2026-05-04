using System.Linq;

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

        PacketBuilder.Send(session, new S_LoginResT
        {
            Success  = success,
            UserInfo = new TUserInfoT
            {
                Id    = success ? session.PlayerId : 0,
                Name  = success ? session.UserName : "",
                Level = success ? session.Player!.Stat.Level : 1,
            },
        }, S_LoginRes.Pack, PacketType.S_LoginRes);

        if (success)
        {
            var snapshot = session.Player!.Inventory.GetSnapshot();
            PacketBuilder.Send(session, new S_InventoryNtfT
            {
                Items = snapshot.Select(kv => new InventoryItemT { ItemId = kv.Key, Amount = kv.Value }).ToList(),
            }, S_InventoryNtf.Pack, PacketType.S_InventoryNtf);
            WorldItemManager.Instance.SyncTo(session);
        }
    }
}
