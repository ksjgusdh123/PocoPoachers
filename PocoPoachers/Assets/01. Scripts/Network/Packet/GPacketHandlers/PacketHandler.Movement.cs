using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnG_Move(FlatPacket root)
    {
        var pkt = root.TypeAsG_Move();
        if (!RoomManager.TryResolveGuestSender(pkt.PlayerId, allowAutoRegister: true, out int guestId))
            return;

        var pos3 = pkt.Pos;
        Vector3 pos = pos3.HasValue
            ? new Vector3(pos3.Value.X, pos3.Value.Y, pos3.Value.Z)
            : Vector3.zero;

        ObjectManager.Instance?.QueueMove(ObjectKind.Player, guestId, pos, pkt.Rotation, pkt.MoveType, pkt.VelocityX, pkt.VelocityZ, pkt.IsSprinting, pkt.IsRolling, pkt.IsAiming, pkt.IsReloading);

        if (RoomManager.IsHost)
        {
            PacketBuilder.BroadcastToGuests(guestId,
                new H_MoveT
                {
                    PlayerId    = guestId,
                    Pos         = pos3.HasValue ? pos3.Value.UnPack() : new Vec3T(),
                    Rotation    = pkt.Rotation,
                    MoveType    = pkt.MoveType,
                    VelocityX   = pkt.VelocityX,
                    VelocityZ   = pkt.VelocityZ,
                    IsSprinting = pkt.IsSprinting,
                    IsRolling   = pkt.IsRolling,
                    IsAiming    = pkt.IsAiming,
                    IsReloading = pkt.IsReloading,
                },
                H_Move.Pack, PacketType.H_Move);
        }
    }
}
