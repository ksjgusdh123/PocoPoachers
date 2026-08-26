using UnityEngine;

public static partial class PacketHandlers
{
    // 원격 사격을 발사한 총의 실제 총알 프리팹/머즐 색으로 재현한다.
    // 총을 찾지 못하는 드문 레이스(발사 패킷이 장착/스폰보다 먼저 도착)에는 공용 프리팹으로 폴백한다.
    static GameObject ResolveBulletPrefab(GunBase gun, BulletPool pool, out Color color)
    {
        if (gun != null && gun.BulletPrefab != null)
        {
            color = gun.Stat != null ? gun.Stat.MuzzleColor : Color.white;
            return gun.BulletPrefab;
        }
        color = Color.white;
        return pool?.NetworkBulletPrefab;
    }

    public static void OnH_Shoot(FlatPacket root)
    {
        var packet = root.TypeAsH_Shoot();
        Vec3? originRaw = packet.Origin;
        Vec3? dirRaw    = packet.Direction;

        Vector3 origin  = originRaw.HasValue ? new Vector3(originRaw.Value.X, originRaw.Value.Y, originRaw.Value.Z) : Vector3.zero;
        Vector3 baseDir = dirRaw.HasValue    ? new Vector3(dirRaw.Value.X,    dirRaw.Value.Y,    dirRaw.Value.Z)    : Vector3.forward;
        if (baseDir == Vector3.zero) baseDir = Vector3.forward;

        var pool = BulletPool.Instance;
        if (pool == null) return;

        GameObject attacker = null;
        float soundRange = 0f;
        GunBase gun = null;
        if (ObjectManager.Instance != null && ObjectManager.Instance.TryGet(ObjectKind.Player, packet.PlayerId, out var shooterObj))
        {
            attacker = shooterObj.gameObject;
            if (GuestValidator.TryGetGuestWeapon(packet.PlayerId, out gun) && gun.Stat != null)
                soundRange = gun.Stat.SoundRange;
        }

        GameObject prefab = ResolveBulletPrefab(gun, pool, out Color bulletColor);
        if (prefab == null) return;

        gun?.PlayFireEffects();

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
            bullet.Initialize(packet.BulletSpeed, packet.Damage, packet.MaxRange, dir, () => pool.Release(prefab, bullet), attacker, bulletColor, packet.IsHeadshot);

            // 쏜 클라가 발급한 순번 — 이 탄환의 명중 통보(H_BulletHit)를 받아 처리하려면 필요하다
            if (i < packet.BulletSeqsLength)
                bullet.SetNetworkId(packet.PlayerId, packet.BulletSeqs(i));
        }

        if (soundRange > 0f)
            SoundEvent.Emit(origin, soundRange, attacker);
    }

    public static void OnH_HitConfirm(FlatPacket root)
    {
        var packet = root.TypeAsH_HitConfirm();
        if (packet.IsKill)
            CrosshairUI.Instance?.ShowHitMarker(true, packet.IsHeadshot);
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

    // 다른 플레이어(또는 내 던지기를 대신 시뮬레이션한 호스트)가 던진 수류탄의 연출 사본.
    // 피해는 호스트가 이미 적용했으므로(OnG_GrenadeThrow) 여기서는 넣지 않는다.
    public static void OnH_GrenadeThrow(FlatPacket root)
    {
        var packet = root.TypeAsH_GrenadeThrow();

        var nm = NetworkManager.Instance;
        if (nm != null && packet.PlayerId == nm.MyPlayerId) return; // 내가 던진 건 이미 로컬 사본이 있다

        PlayerSkillData data = PlayerSkillTable.Instance.Get(packet.SkillId);
        if (data == null) return;

        Vec3? originRaw = packet.Origin;
        Vec3? targetRaw = packet.Target;
        Vector3 origin = originRaw.HasValue ? new Vector3(originRaw.Value.X, originRaw.Value.Y, originRaw.Value.Z) : Vector3.zero;
        Vector3 target = targetRaw.HasValue ? new Vector3(targetRaw.Value.X, targetRaw.Value.Y, targetRaw.Value.Z) : origin;

        GameObject attacker = null;
        if (ObjectManager.Instance != null && ObjectManager.Instance.TryGet(ObjectKind.Player, packet.PlayerId, out var throwerObj))
            attacker = throwerObj.gameObject;

        GrenadeProjectile.Launch(origin, target, attacker, data, applyDamage: false);
    }
}
