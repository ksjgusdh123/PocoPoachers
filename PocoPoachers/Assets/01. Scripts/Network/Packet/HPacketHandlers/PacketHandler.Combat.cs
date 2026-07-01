using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnH_Shoot(FlatPacket root)
    {
        var packet = root.TypeAsH_Shoot();
        Vec3? originRaw = packet.Origin;
        Vec3? dirRaw    = packet.Direction;

        Vector3 origin    = originRaw.HasValue ? new Vector3(originRaw.Value.X, originRaw.Value.Y, originRaw.Value.Z) : Vector3.zero;
        Vector3 direction = dirRaw.HasValue    ? new Vector3(dirRaw.Value.X,    dirRaw.Value.Y,    dirRaw.Value.Z)    : Vector3.forward;
        if (direction == Vector3.zero) direction = Vector3.forward;

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

        var bullet = pool.Get(prefab, origin, Quaternion.LookRotation(direction));
        bullet.Initialize(packet.BulletSpeed, packet.Damage, packet.MaxRange, direction, () => pool.Release(prefab, bullet), attacker);

        if (soundRange > 0f)
            SoundEvent.Emit(origin, soundRange, attacker);
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
