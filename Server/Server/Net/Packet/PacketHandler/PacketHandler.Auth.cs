namespace Server;

public partial class PacketHandler
{
    public void OnC_Login(ClientSession session, FlatPacket root)
    {
        int playerId = SessionManager.Instance.GenerateId();
        session.Player = new Player { PlayerId = playerId };
        SessionManager.Instance.Add(session);

        PacketBuilder.Send(session, new S_LoginResultT
        {
            Success  = true,
            PlayerId = playerId
        }, S_LoginResult.Pack, PacketType.S_LoginResult);
    }

    public void OnC_Logout(ClientSession session, FlatPacket root)
    {
        SessionManager.Instance.Remove(session);
    }
}
