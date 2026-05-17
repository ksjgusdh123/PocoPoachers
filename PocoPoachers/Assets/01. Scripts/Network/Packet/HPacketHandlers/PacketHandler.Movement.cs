using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnH_Move(FlatPacket root)
    {
        var pkt = root.TypeAsH_Move();
        var pos3 = pkt.Pos;
        Vector3 pos = pos3.HasValue
            ? new Vector3(pos3.Value.X, pos3.Value.Y, pos3.Value.Z)
            : Vector3.zero;

        ObjectManager.Instance?.QueueMove(ObjectKind.Player, pkt.PlayerId, pos, pkt.Rotation, pkt.MoveType, pkt.VelocityX, pkt.VelocityZ, pkt.IsSprinting);
    }
}
