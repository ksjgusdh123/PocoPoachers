namespace Server;

public partial class PacketHandler
{
    public void OnC_ShootReq(ClientSession session, FlatPacket root)
    {
        var pkt = root.TypeAsC_ShootReq();
        if (session.Player is not { } player)
            return;

        if (!pkt.Origin.HasValue || !pkt.Direction.HasValue)
            return;

        PacketSender.SShootNtfBroadcast(
            session,
            player.PlayerId,
            pkt.Origin.Value.X, pkt.Origin.Value.Y, pkt.Origin.Value.Z,
            pkt.Direction.Value.X, pkt.Direction.Value.Y, pkt.Direction.Value.Z,
            pkt.BulletSpeed,
            pkt.Damage,
            pkt.MaxRange);
    }
}
