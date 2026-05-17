using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnG_Move(FlatPacket root)
    {
        var pkt = root.TypeAsG_Move();
        var pos3 = pkt.Pos;
        Vector3 pos = pos3.HasValue
            ? new Vector3(pos3.Value.X, pos3.Value.Y, pos3.Value.Z)
            : Vector3.zero;

        ObjectManager.Instance?.QueueMove(ObjectKind.Player, pkt.PlayerId, pos, pkt.Rotation, pkt.MoveType, pkt.VelocityX, pkt.VelocityZ, pkt.IsSprinting);

        if (RoomManager.IsHost && RoomManager.LastGuestId == 0)
            RoomManager.Instance?.TryAutoRegisterGuest(pkt.PlayerId);

        if (RoomManager.IsHost)
        {
            PacketBuilder.BroadcastToGuests(pkt.PlayerId,
                new H_MoveT
                {
                    PlayerId    = pkt.PlayerId,
                    Pos         = pos3.HasValue ? pos3.Value.UnPack() : new Vec3T(),
                    Rotation    = pkt.Rotation,
                    MoveType    = pkt.MoveType,
                    VelocityX   = pkt.VelocityX,
                    VelocityZ   = pkt.VelocityZ,
                    IsSprinting = pkt.IsSprinting,
                },
                H_Move.Pack, PacketType.H_Move);
        }
    }
}
