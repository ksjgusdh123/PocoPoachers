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

        ObjectManager.Instance?.QueueMove(ObjectKind.Player, pkt.PlayerId, pos, pkt.Rotation, pkt.MoveType);

        // 호스트는 해당 게스트의 이동을 나머지 게스트에게 H_Move로 릴레이
        var p2p = P2PManager.Instance;
        if (p2p != null && p2p.IsHost)
        {
            var relay = PacketBuilder.BuildSegment(
                new H_MoveT
                {
                    PlayerId = pkt.PlayerId,
                    Pos      = pos3.HasValue ? pos3.Value.UnPack() : new Vec3T(),
                    Rotation = pkt.Rotation,
                    MoveType = pkt.MoveType,
                },
                H_Move.Pack, PacketType.H_Move);
            p2p.RelayExcept(pkt.PlayerId, relay);
        }
    }
}
