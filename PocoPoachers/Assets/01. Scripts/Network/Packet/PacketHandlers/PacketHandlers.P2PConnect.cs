public static partial class PacketHandlers
{
    public static void OnS_RegisterP2PSessionRes(FlatPacket root)
    {
        var pkt = root.TypeAsS_RegisterP2PSessionRes();
        P2PManager.Instance.OnRegistered(pkt.Success);
    }

    public static void OnS_P2PSessionInfo(FlatPacket root)
    {
        var pkt = root.TypeAsS_P2PSessionInfo();
        P2PManager.Instance.OnSessionInfo(
            pkt.HostPublicIp,  pkt.HostPublicPort,
            pkt.HostPrivateIp, pkt.HostPrivatePort);
    }

    public static void OnS_PeerJoined(FlatPacket root)
    {
        var pkt = root.TypeAsS_PeerJoined();
        P2PManager.Instance.OnPeerJoined(pkt.PeerPublicIp, pkt.PeerPublicPort);
    }
}
