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

    // 호스트가 권위 수류탄을 스폰했다는 통보 — 던진 게스트 본인은 스킵 전송으로 받지 않으므로
    // (이미 로컬 예측 사본이 있다) 여기 도달하는 건 항상 "남이 던진" 수류탄이다.
    // 물리 없는 보간 사본을 만들고, 이후 위치/폭발은 grenade_id로 OnH_GrenadeMove/Explode가 갱신한다.
    public static void OnH_GrenadeThrow(FlatPacket root)
    {
        var packet = root.TypeAsH_GrenadeThrow();

        PlayerSkillData data = PlayerSkillTable.Instance.Get(packet.SkillId);
        if (data == null) return;

        Vec3? originRaw = packet.Origin;
        Vector3 origin = originRaw.HasValue ? new Vector3(originRaw.Value.X, originRaw.Value.Y, originRaw.Value.Z) : Vector3.zero;

        GrenadeProjectile.SpawnRemote(packet.GrenadeId, origin, data);
    }

    // 호스트가 물리로 시뮬레이션 중인 권위 수류탄의 위치 — 보간 사본을 그쪽으로 계속 당긴다.
    public static void OnH_GrenadeMove(FlatPacket root)
    {
        var packet = root.TypeAsH_GrenadeMove();
        Vec3? posRaw = packet.Pos;
        if (!posRaw.HasValue) return;

        GrenadeProjectile.OnNetMove(packet.GrenadeId, new Vector3(posRaw.Value.X, posRaw.Value.Y, posRaw.Value.Z));
    }

    // 호스트가 판정한 폭발 위치 — 피해는 이미 적용됐으므로 여기서는 연출만 재생한다.
    public static void OnH_GrenadeExplode(FlatPacket root)
    {
        var packet = root.TypeAsH_GrenadeExplode();
        Vec3? posRaw = packet.Pos;
        Vector3 pos = posRaw.HasValue ? new Vector3(posRaw.Value.X, posRaw.Value.Y, posRaw.Value.Z) : Vector3.zero;

        GrenadeProjectile.OnNetExplode(packet.GrenadeId, pos);
    }
}
