namespace Server;

public partial class PacketHandler
{
    public void OnC_PingReq(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_PingReq();
        PacketBuilder.Send(session, new S_PongResT { SendTime = pkt.SendTime }, S_PongRes.Pack, PacketType.S_PongRes);
    }
}
