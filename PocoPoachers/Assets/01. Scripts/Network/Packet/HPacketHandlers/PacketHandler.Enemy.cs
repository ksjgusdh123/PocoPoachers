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

    public static void OnH_EnemySpeak(FlatPacket root)
    {
        var packet = root.TypeAsH_EnemySpeak();
        EnemyNetSync.OnNetSpeak(packet.EnemyId, packet.Message, packet.Duration);
    }

    // 적 총알을 게스트에 스폰 — attacker를 발사한 적(Enemy 레이어)으로 지정해야 같은 레이어(적끼리) 스킵이 동작한다.
    // (플레이어 사격 경로와 달리 PlayerId가 아닌 EnemyId로 발사자를 해석)
    public static void OnH_EnemyShoot(FlatPacket root)
    {
        var packet = root.TypeAsH_EnemyShoot();
        Vec3? originRaw = packet.Origin;
        Vec3? dirRaw    = packet.Direction;

        Vector3 origin  = originRaw.HasValue ? new Vector3(originRaw.Value.X, originRaw.Value.Y, originRaw.Value.Z) : Vector3.zero;
        Vector3 baseDir = dirRaw.HasValue    ? new Vector3(dirRaw.Value.X,    dirRaw.Value.Y,    dirRaw.Value.Z)    : Vector3.forward;
        if (baseDir == Vector3.zero) baseDir = Vector3.forward;

        var pool = BulletPool.Instance;
        if (pool == null) return;

        GameObject attacker = null;
        GunBase gun = null;
        if (EnemyNetSync.TryGetGameObject(packet.EnemyId, out var enemyGo))
        {
            attacker = enemyGo;
            gun = enemyGo.GetComponent<AIWeaponController>()?.Gun;
        }

        GameObject prefab = ResolveBulletPrefab(gun, pool, out Color bulletColor);
        if (prefab == null) return;

        gun?.PlayMuzzleFlash();

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
            bullet.Initialize(packet.BulletSpeed, packet.Damage, packet.MaxRange, dir, () => pool.Release(prefab, bullet), attacker, bulletColor);
        }
    }
}
