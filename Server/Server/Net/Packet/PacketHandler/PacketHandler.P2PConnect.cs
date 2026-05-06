namespace Server;

public partial class PacketHandler
{
    public void OnC_RegisterP2PSession(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_RegisterP2PSession();
        bool success = RoomManager.Instance.TryRegister(
            pkt.SessionCode,
            session,
            pkt.PublicIp, pkt.PublicPort,
            pkt.PrivateIp, pkt.PrivatePort);

        PacketBuilder.Send(session,
            new S_RegisterP2PSessionResT { Success = success },
            S_RegisterP2PSessionRes.Pack,
            PacketType.S_RegisterP2PSessionRes);
    }

    public void OnC_JoinP2PSession(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_JoinP2PSession();
        var room = RoomManager.Instance.TryJoin(pkt.SessionCode, session);

        if (room == null) return;

        // 피어에게 호스트 엔드포인트 전달
        PacketBuilder.Send(session,
            new S_P2PSessionInfoT
            {
                HostPublicIp = room.HostPublicIp,
                HostPublicPort = room.HostPublicPort,
                HostPrivateIp = room.HostPrivateIp,
                HostPrivatePort = room.HostPrivatePort,
            },
            S_P2PSessionInfo.Pack,
            PacketType.S_P2PSessionInfo);

        // 호스트에게 피어 공개 주소 전달
        // peer의 공개 IP는 TCP 연결 RemoteEndPoint에서 추출
        string peerPublicIp = session.RemoteEndPoint?.Address.ToString() ?? "";
        ushort peerPublicPort = (ushort)(session.RemoteEndPoint?.Port ?? 0);

        PacketBuilder.Send(room.Host,
            new S_PeerJoinedT
            {
                PeerPublicIp = peerPublicIp,
                PeerPublicPort = peerPublicPort,
            },
            S_PeerJoined.Pack,
            PacketType.S_PeerJoined);
    }
}
