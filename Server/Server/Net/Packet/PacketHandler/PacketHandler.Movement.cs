namespace Server;

public partial class PacketHandler
{
    public void OnC_MoveReq(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_MoveReq();
        if (session.Player is not { } player)
            return;

        var pos = pkt.Pos;
        if (!pos.HasValue)
            return;

        float x = pos.Value.X;
        float y = pos.Value.Y;
        float z = pos.Value.Z;
        float rotation = pkt.Rotation;
        sbyte moveType = pkt.MoveType;

        PacketSender.SMoveNtfBroadcast(session, player.PlayerId, x, y, z, rotation, moveType);
    }
}
