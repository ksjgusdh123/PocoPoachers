using UnityEngine;

public static partial class PacketHandlers
{
    // 호스트가 판정한 탄환 명중. 게스트는 적 충돌을 스스로 판정하지 않으므로
    // 혈흔과 탄환 소멸이 이 경로로만 일어난다.
    public static void OnH_BulletHit(FlatPacket root)
    {
        var packet = root.TypeAsH_BulletHit();

        Vec3? pos = packet.Pos;
        Vec3? normal = packet.Normal;
        if (!pos.HasValue || !normal.HasValue) return;

        Bullet.ApplyNetworkHit(
            packet.ShooterId,
            packet.Seq,
            new Vector3(pos.Value.X, pos.Value.Y, pos.Value.Z),
            new Vector3(normal.Value.X, normal.Value.Y, normal.Value.Z),
            packet.IsKill,
            packet.IsHeadshot);
    }
}
