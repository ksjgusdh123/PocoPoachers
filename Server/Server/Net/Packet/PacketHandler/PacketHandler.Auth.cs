namespace Server;

public partial class PacketHandler
{
    public void OnC_Login(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_Login();
        string userName = pkt.Username ?? string.Empty;
        bool success = !string.IsNullOrWhiteSpace(userName);
        int playerId = -1;

        if (success)
        {
            playerId = SessionManager.Instance.GenerateId();
            session.Player = new Player { PlayerId = playerId, UserName = userName };
            SessionManager.Instance.Add(session);
        }

        PacketBuilder.Send(session, new S_LoginResultT
        {
            Success  = success,
            PlayerId = playerId
        }, S_LoginResult.Pack, PacketType.S_LoginResult);
    }

    public void OnC_Logout(ClientSession session, FlatPacket root)
    {
        SessionManager.Instance.Remove(session);
    }
}
