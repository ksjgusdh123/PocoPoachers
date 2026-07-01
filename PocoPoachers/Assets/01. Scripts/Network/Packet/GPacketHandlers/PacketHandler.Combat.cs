using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnG_Shoot(FlatPacket root)
    {
        var pkt = root.TypeAsG_Shoot();
        if (!RoomManager.TryResolveGuestSender(pkt.PlayerId, allowAutoRegister: false, out int guestId))
            return;

        Vec3? originRaw = pkt.Origin;
        Vec3? dirRaw    = pkt.Direction;

        Vector3 origin    = originRaw.HasValue ? new Vector3(originRaw.Value.X, originRaw.Value.Y, originRaw.Value.Z) : Vector3.zero;
        Vector3 direction = dirRaw.HasValue    ? new Vector3(dirRaw.Value.X,    dirRaw.Value.Y,    dirRaw.Value.Z)    : Vector3.forward;
        if (direction == Vector3.zero) direction = Vector3.forward;

        float bulletSpeed = pkt.BulletSpeed;
        float damage      = pkt.Damage;
        float maxRange    = pkt.MaxRange;
        float soundRange  = pkt.SoundRange;

        if (NetworkPlayerAuthority.TryGetGuestGun(guestId, out var gun) && gun.Stat != null)
        {
            bulletSpeed = gun.Stat.BulletSpeed;
            damage      = gun.Stat.Damage;
            maxRange    = gun.Stat.BulletRange;
            soundRange  = gun.Stat.SoundRange;
        }

        var pool = BulletPool.Instance;
        var prefab = pool?.NetworkBulletPrefab;
        if (prefab == null) return;

        GameObject attacker = null;
        if (ObjectManager.Instance != null && ObjectManager.Instance.TryGet(ObjectKind.Player, guestId, out var shooterObj))
            attacker = shooterObj.gameObject;

        var bullet = pool.Get(prefab, origin, Quaternion.LookRotation(direction));
        bullet.Initialize(bulletSpeed, damage, maxRange, direction, () => pool.Release(prefab, bullet), attacker);

        if (RoomManager.IsHost)
        {
            if (soundRange > 0f)
                SoundEvent.Emit(origin, soundRange, attacker);

            PacketBuilder.BroadcastToGuests(guestId,
                new H_ShootT
                {
                    PlayerId    = guestId,
                    Origin      = originRaw.HasValue ? originRaw.Value.UnPack() : new Vec3T(),
                    Direction   = dirRaw.HasValue    ? dirRaw.Value.UnPack()    : new Vec3T(),
                    BulletSpeed = bulletSpeed,
                    Damage      = damage,
                    MaxRange    = maxRange,
                },
                H_Shoot.Pack, PacketType.H_Shoot);
        }
    }
}
