using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnH_Shoot(FlatPacket root)
    {
        var packet = root.TypeAsH_Shoot();
        Vec3? originRaw = packet.Origin;
        Vec3? dirRaw    = packet.Direction;

        Vector3 origin  = originRaw.HasValue ? new Vector3(originRaw.Value.X, originRaw.Value.Y, originRaw.Value.Z) : Vector3.zero;
        Vector3 baseDir = dirRaw.HasValue    ? new Vector3(dirRaw.Value.X,    dirRaw.Value.Y,    dirRaw.Value.Z)    : Vector3.forward;
        if (baseDir == Vector3.zero) baseDir = Vector3.forward;

        var pool   = BulletPool.Instance;
        var prefab = pool?.NetworkBulletPrefab;
        if (prefab == null) return;

        GameObject attacker = null;
        float soundRange = 0f;
        if (ObjectManager.Instance != null && ObjectManager.Instance.TryGet(ObjectKind.Player, packet.PlayerId, out var shooterObj))
        {
            attacker = shooterObj.gameObject;
            if (GuestValidator.TryGetGuestWeapon(packet.PlayerId, out var gun) && gun.Stat != null)
                soundRange = gun.Stat.SoundRange;
        }

        // 다발(샷건) 방향이 있으면 펠릿별로, 없으면 baseDir 단발로 스폰
        int pelletCount = Mathf.Max(1, packet.DirectionsLength);
        for (int i = 0; i < pelletCount; i++)
        {
            Vector3 dir = baseDir;
            if (packet.DirectionsLength > 0)
            {
                Vec3? p = packet.Directions(i);
                if (p.HasValue) dir = new Vector3(p.Value.X, p.Value.Y, p.Value.Z);
                if (dir == Vector3.zero) dir = baseDir;
            }

            var bullet = pool.Get(prefab, origin, Quaternion.LookRotation(dir));
            bullet.Initialize(packet.BulletSpeed, packet.Damage, packet.MaxRange, dir, () => pool.Release(prefab, bullet), attacker);
        }

        if (soundRange > 0f)
            SoundEvent.Emit(origin, soundRange, attacker);
    }

    public static void OnH_ShootRejected(FlatPacket root)
    {
        var packet = root.TypeAsH_ShootRejected();

        MainThreadDispatcher.Enqueue(() =>
        {
            GunBase gun = EquippableItemBase.FindByUid(packet.ItemUid) as GunBase;
            if (gun == null)
            {
                var weapon = UnityEngine.Object.FindAnyObjectByType<WeaponController>();
                gun = weapon?.CurrentGun;
            }
            if (gun == null) return;

            gun.ApplyHostShootState(packet.CurrentAmmo, packet.CurrentDurability);
        });
    }

    public static void OnH_SandbagDestroy(FlatPacket root)
    {
        var packet = root.TypeAsH_SandbagDestroy();
        int id = packet.SandbagId;

        MainThreadDispatcher.Enqueue(() =>
        {
            Sandbag.Find(id)?.DestroyFromNetwork();
        });
    }
}
