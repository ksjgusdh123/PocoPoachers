using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnH_Move(FlatPacket root)
    {
        var packet = root.TypeAsH_Move();
        var pos3 = packet.Pos;
        Vector3 pos = pos3.HasValue
            ? new Vector3(pos3.Value.X, pos3.Value.Y, pos3.Value.Z)
            : Vector3.zero;

        ObjectManager.Instance?.QueueMove(ObjectKind.Player, packet.PlayerId, pos, packet.Rotation, packet.MoveType, packet.VelocityX, packet.VelocityZ, packet.IsSprinting, packet.IsRolling, packet.IsAiming, packet.IsReloading, packet.IsDown);
    }
}
