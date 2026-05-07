namespace Server;

public partial class PacketHandler
{
    public void OnC_Heartbeat(ClientSession session, FlatPacket root)
    {
        session.LastPingAt = DateTimeOffset.UtcNow;
        var pkt = root.TypeAsC_Heartbeat();
        PacketBuilder.Send(session, new S_HeartbeatAckT { SendTime = pkt.SendTime }, S_HeartbeatAck.Pack, PacketType.S_HeartbeatAck);
    }
}
