namespace Server;

public partial class PacketHandler
{
    public void OnC_MoveReq(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_MoveReq();
        if (session.Player is not { } player)
            return;

        if (!pkt.Pos.HasValue)
            return;

        var pos = pkt.Pos.Value;
        PacketBuilder.Broadcast(SessionManager.Instance.Snapshot(session), new S_MoveNtfT
        {
            PlayerId = player.PlayerId,
            Pos      = new Vec3T { X = pos.X, Y = pos.Y, Z = pos.Z },
            Rotation = pkt.Rotation,
            MoveType = pkt.MoveType,
        }, S_MoveNtf.Pack, PacketType.S_MoveNtf);
    }
}
