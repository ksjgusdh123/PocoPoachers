using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnH_EnemySpawn(FlatPacket root)
    {
        var pkt = root.TypeAsH_EnemySpawn();
        Vec3? posRaw = pkt.Pos;
        var pos = posRaw.HasValue ? new Vector3(posRaw.Value.X, posRaw.Value.Y, posRaw.Value.Z) : Vector3.zero;
        EnemyNetSync.OnNetSpawn(pkt.EnemyId, pos, pkt.Rotation, pkt.Hp, pkt.MaxHp);
    }

    public static void OnH_EnemyMove(FlatPacket root)
    {
        var pkt = root.TypeAsH_EnemyMove();
        Vec3? posRaw = pkt.Pos;
        var pos = posRaw.HasValue ? new Vector3(posRaw.Value.X, posRaw.Value.Y, posRaw.Value.Z) : Vector3.zero;
        EnemyNetSync.OnNetMove(pkt.EnemyId, pos, pkt.Rotation);
    }

    public static void OnH_EnemyHit(FlatPacket root)
    {
        var pkt = root.TypeAsH_EnemyHit();
        EnemyNetSync.OnNetHit(pkt.EnemyId, pkt.Hp, pkt.MaxHp);
    }

    public static void OnH_EnemyDie(FlatPacket root)
    {
        var pkt = root.TypeAsH_EnemyDie();
        EnemyNetSync.OnNetDie(pkt.EnemyId);
    }
}
