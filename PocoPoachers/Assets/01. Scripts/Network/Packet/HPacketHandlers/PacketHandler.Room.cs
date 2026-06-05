using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnH_GuestJoined(FlatPacket root)
    {
        var pkt = root.TypeAsH_GuestJoined();
        for (int i = 0; i < pkt.InfoLength; i++)
        {
            if (!pkt.Info(i).HasValue) continue;
            var info = pkt.Info(i).Value;
            var p = info.Pos;
            ObjectManager.Instance?.QueueMove(
                ObjectKind.Player, info.PlayerId,
                new Vector3(p.X, p.Y, p.Z), info.Rotation, 0);
        }
    }

    public static void OnH_Leave(FlatPacket root)
    {
        var pkt = root.TypeAsH_Leave();
        if (pkt.IsHost)
            RoomManager.Instance?.HandleHostLeft();
        else
            ObjectManager.Instance?.Despawn(ObjectKind.Player, pkt.PlayerId);
    }
}
