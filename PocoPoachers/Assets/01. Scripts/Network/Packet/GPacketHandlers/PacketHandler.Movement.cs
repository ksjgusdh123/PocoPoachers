using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnG_Move(FlatPacket root)
    {
        var packet = root.TypeAsG_Move();
        if (!RoomManager.TryGetGuestIdFromPacket(packet.PlayerId, autoRegister: true, out int guestId))
            return;

        var pos3 = packet.Pos;
        Vector3 pos = pos3.HasValue
            ? new Vector3(pos3.Value.X, pos3.Value.Y, pos3.Value.Z)
            : Vector3.zero;

        ObjectManager.Instance?.QueueMove(ObjectKind.Player, guestId, pos, packet.Rotation, packet.MoveType, packet.VelocityX, packet.VelocityZ, packet.IsSprinting, packet.IsRolling, packet.IsAiming, packet.IsReloading);

        if (RoomManager.IsHost)
        {
            PacketBuilder.BroadcastToGuests(guestId,
                new H_MoveT
                {
                    PlayerId    = guestId,
                    Pos         = pos3.HasValue ? pos3.Value.UnPack() : new Vec3T(),
                    Rotation    = packet.Rotation,
                    MoveType    = packet.MoveType,
                    VelocityX   = packet.VelocityX,
                    VelocityZ   = packet.VelocityZ,
                    IsSprinting = packet.IsSprinting,
                    IsRolling   = packet.IsRolling,
                    IsAiming    = packet.IsAiming,
                    IsReloading = packet.IsReloading,
                },
                H_Move.Pack, PacketType.H_Move);
        }
    }
}
