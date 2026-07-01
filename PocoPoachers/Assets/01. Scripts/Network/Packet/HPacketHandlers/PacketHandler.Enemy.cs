using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnH_EnemySpawn(FlatPacket root)
    {
        var packet = root.TypeAsH_EnemySpawn();
        Vec3? posRaw = packet.Pos;
        var pos = posRaw.HasValue ? new Vector3(posRaw.Value.X, posRaw.Value.Y, posRaw.Value.Z) : Vector3.zero;
        EnemyNetSync.OnNetSpawn(packet.EnemyId, packet.EnemyTypeId, pos, packet.Rotation, packet.Hp, packet.MaxHp, packet.WeaponId, packet.HelmetId);
    }

    public static void OnH_EnemyMove(FlatPacket root)
    {
        var packet = root.TypeAsH_EnemyMove();
        Vec3? posRaw = packet.Pos;
        var pos = posRaw.HasValue ? new Vector3(posRaw.Value.X, posRaw.Value.Y, posRaw.Value.Z) : Vector3.zero;
        EnemyNetSync.OnNetMove(packet.EnemyId, pos, packet.Rotation, packet.AnimState);
    }

    public static void OnH_EnemyHit(FlatPacket root)
    {
        var packet = root.TypeAsH_EnemyHit();
        EnemyNetSync.OnNetHit(packet.EnemyId, packet.Hp, packet.MaxHp, packet.Damage);
    }

    public static void OnH_EnemyDie(FlatPacket root)
    {
        var packet = root.TypeAsH_EnemyDie();
        EnemyNetSync.OnNetDie(packet.EnemyId);
    }
}
