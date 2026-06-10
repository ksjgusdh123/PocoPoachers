using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnG_Shoot(FlatPacket root)
    {
        var pkt = root.TypeAsG_Shoot();
        Vec3? originRaw = pkt.Origin;
        Vec3? dirRaw    = pkt.Direction;

        Vector3 origin    = originRaw.HasValue ? new Vector3(originRaw.Value.X, originRaw.Value.Y, originRaw.Value.Z) : Vector3.zero;
        Vector3 direction = dirRaw.HasValue    ? new Vector3(dirRaw.Value.X,    dirRaw.Value.Y,    dirRaw.Value.Z)    : Vector3.forward;
        if (direction == Vector3.zero) direction = Vector3.forward;

        var pool = BulletPool.Instance;
        var prefab = pool?.NetworkBulletPrefab;
        if (prefab == null) return;

        var bullet = pool.Get(prefab, origin, Quaternion.LookRotation(direction));
        bullet.Initialize(pkt.BulletSpeed, pkt.Damage, pkt.MaxRange, direction, () => pool.Release(prefab, bullet));

        if (RoomManager.IsHost)
        {
            if (pkt.SoundRange > 0f)
                SoundEvent.Emit(origin, pkt.SoundRange, null);

            PacketBuilder.BroadcastToGuests(pkt.PlayerId,
                new H_ShootT
                {
                    PlayerId    = pkt.PlayerId,
                    Origin      = originRaw.HasValue ? originRaw.Value.UnPack() : new Vec3T(),
                    Direction   = dirRaw.HasValue    ? dirRaw.Value.UnPack()    : new Vec3T(),
                    BulletSpeed = pkt.BulletSpeed,
                    Damage      = pkt.Damage,
                    MaxRange    = pkt.MaxRange,
                },
                H_Shoot.Pack, PacketType.H_Shoot);
        }
    }
}
