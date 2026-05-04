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

        var o = pkt.Origin.Value;
        var d = pkt.Direction.Value;
        PacketBuilder.Broadcast(SessionManager.Instance.Snapshot(session), new S_ShootNtfT
        {
            PlayerId    = player.PlayerId,
            Origin      = new Vec3T { X = o.X, Y = o.Y, Z = o.Z },
            Direction   = new Vec3T { X = d.X, Y = d.Y, Z = d.Z },
            BulletSpeed = pkt.BulletSpeed,
            Damage      = pkt.Damage,
            MaxRange    = pkt.MaxRange,
        }, S_ShootNtf.Pack, PacketType.S_ShootNtf);
    }
}
