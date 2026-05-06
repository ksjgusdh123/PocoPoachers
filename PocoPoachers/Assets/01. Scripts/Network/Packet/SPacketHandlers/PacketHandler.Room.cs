public static partial class PacketHandlers
{
    public static void OnS_CreateRoom(FlatPacket root)
    {
        var pkt = root.TypeAsS_CreateRoom();
        string code = pkt.SessionCode ?? string.Empty;
        bool success = pkt.Success;

        MainThreadDispatcher.Enqueue(() =>
        {
            SessionCodeUI.Instance?.HandleCreateRoom(code, success);
        });
    }

    public static void OnS_JoinRoom(FlatPacket root)
    {
        var pkt = root.TypeAsS_JoinRoom();
        bool success = pkt.Success;
        PeerInfoT hostInfo = pkt.HostInfo.HasValue ? pkt.HostInfo.Value.UnPack() : null;

        MainThreadDispatcher.Enqueue(() =>
        {
            SessionCodeUI.Instance?.HandleJoinRoom(success);
            if (!success || hostInfo == null)
                P2PManager.Instance?.HandleFailure("입장 실패");
            else
                P2PManager.Instance?.BeginPunch(hostInfo);
        });
    }

    public static void OnS_PeerJoined(FlatPacket root)
    {
        var pkt = root.TypeAsS_PeerJoined();
        PeerInfoT guestInfo = pkt.Info.HasValue ? pkt.Info.Value.UnPack() : null;

        MainThreadDispatcher.Enqueue(() =>
        {
            if (guestInfo != null)
                P2PManager.Instance?.BeginPunch(guestInfo);
        });
    }
}
