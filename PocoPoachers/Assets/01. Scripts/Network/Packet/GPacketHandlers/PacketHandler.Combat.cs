using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnG_Shoot(FlatPacket root)
    {
        var packet = root.TypeAsG_Shoot();
        if (!RoomManager.TryGetGuestIdFromPacket(packet.PlayerId, autoRegister: false, out int guestId))
            return;

        if (!GuestValidator.TryGetGuestWeapon(guestId, out var gun))
            return;

        if (!gun.TryAuthorizeHostShot())
        {
            PacketBuilder.SendReliableToGuest(guestId, new H_ShootRejectedT
            {
                ItemUid           = gun.Uid,
                CurrentAmmo       = gun.CurrentAmmo,
                CurrentDurability = gun.CurrentDurability,
            }, H_ShootRejected.Pack, PacketType.H_ShootRejected);
            return;
        }

        Vec3? originRaw = packet.Origin;
        Vec3? dirRaw    = packet.Direction;

        Vector3 origin    = originRaw.HasValue ? new Vector3(originRaw.Value.X, originRaw.Value.Y, originRaw.Value.Z) : Vector3.zero;
        Vector3 direction = dirRaw.HasValue    ? new Vector3(dirRaw.Value.X,    dirRaw.Value.Y,    dirRaw.Value.Z)    : Vector3.forward;
        if (direction == Vector3.zero) direction = Vector3.forward;

        float bulletSpeed = gun.Stat.BulletSpeed;
        float damage      = gun.Stat.Damage;
        float maxRange    = gun.Stat.BulletRange;
        float soundRange  = gun.Stat.SoundRange;

        if (gun.Uid != 0)
        {
            var (current, max) = WorldEquipmentManager.ApplyChange(gun.Uid, gun.ItemId, -gun.DurabilityPerShot, gun.MaxDurability);
            gun.SetDurability(current);
            PacketBuilder.BroadcastToGuests(new H_DurabilityT { ItemUid = gun.Uid, Current = current, Max = max },
                H_Durability.Pack, PacketType.H_Durability);
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
