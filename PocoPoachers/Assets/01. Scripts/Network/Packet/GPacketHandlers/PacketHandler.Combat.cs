using System.Collections.Generic;
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

        Vector3 origin  = originRaw.HasValue ? new Vector3(originRaw.Value.X, originRaw.Value.Y, originRaw.Value.Z) : Vector3.zero;
        Vector3 baseDir = dirRaw.HasValue    ? new Vector3(dirRaw.Value.X,    dirRaw.Value.Y,    dirRaw.Value.Z)    : Vector3.forward;
        if (baseDir == Vector3.zero) baseDir = Vector3.forward;

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
        if (pool == null) return;
        GameObject prefab = ResolveBulletPrefab(gun, pool, out Color bulletColor);
        if (prefab == null) return;

        gun.PlayFireEffects();

        GameObject attacker = null;
        // 크리 배율은 쏜 게스트의 스탯에서 읽는다 — 데미지를 넣는 건 이 권위 탄환이다
        float critMultiplier = StatBase.DefaultCritMultiplier;
        if (ObjectManager.Instance != null && ObjectManager.Instance.TryGet(ObjectKind.Player, guestId, out var shooterObj))
        {
            attacker = shooterObj.gameObject;
            if (shooterObj.TryGetComponent<StatBase>(out var shooterStat))
            {
                critMultiplier = shooterStat.CritMultiplier;

                // 사거리도 게스트가 보낸 packet.MaxRange가 아니라
                // 호스트가 아는 총 스탯 × 호스트가 검증해둔 배율로 계산한다
                maxRange *= shooterStat.RangeMultiplier;
            }
        }

        // 다발(샷건) 방향이 있으면 펠릿별로, 없으면 baseDir 단발로 권위 있는 총알을 스폰한다.
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

            // 쏜 게스트가 발급한 순번을 그대로 쓴다 — 명중 통보를 그 게스트가 자기 탄환과 맞춰야 한다
            if (i < packet.BulletSeqsLength)
                bullet.SetNetworkId(guestId, packet.BulletSeqs(i));

            bullet.SetCritMultiplier(critMultiplier);

            bullet.Initialize(bulletSpeed, damage, maxRange, dir, () => pool.Release(prefab, bullet), attacker, bulletColor, packet.IsHeadshot,
                onDamageResult: (isKill, target) =>
                {
                    // 게스트가 추가탄 드론을 켜두었으면 호스트가 대신 유도탄을 쏜다.
                    // 데미지는 호스트에서만 적용되므로 이 경로가 없으면 게스트의 드론은 연출뿐이다.
                    CombatDrone.FindFor(guestId)?.TryFireAt(target);

                    // 게스트 본인의 로컬(연출용) 총알은 데미지를 적용하지 않아 킬 여부를 모르므로,
                    // 호스트가 대신 시뮬레이션한 권위 총알의 결과를 쏜 게스트에게 돌려줘 히트마커를 빨간색으로 확인시킨다
                    if (isKill)
                        PacketBuilder.SendReliableToGuest(guestId,
                            new H_HitConfirmT { IsKill = true, IsHeadshot = packet.IsHeadshot },
                            H_HitConfirm.Pack, PacketType.H_HitConfirm);
                });
        }

        if (RoomManager.IsHost)
        {
            if (soundRange > 0f)
                SoundEvent.Emit(origin, soundRange, attacker);

            List<Vec3T> rebroadcastDirs = null;
            if (packet.DirectionsLength > 0)
            {
                rebroadcastDirs = new List<Vec3T>(packet.DirectionsLength);
                for (int i = 0; i < packet.DirectionsLength; i++)
                {
                    Vec3? d = packet.Directions(i);
                    rebroadcastDirs.Add(d.HasValue ? d.Value.UnPack() : new Vec3T());
                }
            }

            List<int> rebroadcastSeqs = null;
            if (packet.BulletSeqsLength > 0)
            {
                rebroadcastSeqs = new List<int>(packet.BulletSeqsLength);
                for (int i = 0; i < packet.BulletSeqsLength; i++)
                    rebroadcastSeqs.Add(packet.BulletSeqs(i));
            }

            PacketBuilder.BroadcastToGuests(guestId,
                new H_ShootT
                {
                    PlayerId    = guestId,
                    Origin      = originRaw.HasValue ? originRaw.Value.UnPack() : new Vec3T(),
                    Direction   = dirRaw.HasValue    ? dirRaw.Value.UnPack()    : new Vec3T(),
                    BulletSpeed = bulletSpeed,
                    Damage      = damage,
                    MaxRange    = maxRange,
                    Directions  = rebroadcastDirs,
                    IsHeadshot  = packet.IsHeadshot,
                    BulletSeqs  = rebroadcastSeqs,
                },
                H_Shoot.Pack, PacketType.H_Shoot);
        }
    }

    // 게스트가 던진 수류탄 — 호스트가 대신 시뮬레이션하는 권위 사본을 스폰해 실제 폭발 피해를 넣고,
    // 결과를 (보낸 게스트를 뺀) 나머지 게스트에게 다시 뿌려 연출을 맞춘다.
    // 데미지·반경·퓨즈는 게스트 패킷값을 신뢰하지 않고 skill_id로 테이블에서 직접 읽는다(G_Shoot과 같은 이유).
    public static void OnG_GrenadeThrow(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId))
            return;

        var packet = root.TypeAsG_GrenadeThrow();
        PlayerSkillData data = PlayerSkillTable.Instance.Get(packet.SkillId);
        if (data == null) return;

        Vec3? originRaw = packet.Origin;
        Vec3? targetRaw = packet.Target;
        Vector3 origin = originRaw.HasValue ? new Vector3(originRaw.Value.X, originRaw.Value.Y, originRaw.Value.Z) : Vector3.zero;
        Vector3 target = targetRaw.HasValue ? new Vector3(targetRaw.Value.X, targetRaw.Value.Y, targetRaw.Value.Z) : origin;

        GameObject attacker = null;
        if (ObjectManager.Instance != null && ObjectManager.Instance.TryGet(ObjectKind.Player, guestId, out var throwerObj))
            attacker = throwerObj.gameObject;

        GrenadeProjectile.Launch(origin, target, attacker, data, applyDamage: true);

        PacketBuilder.BroadcastReliableToGuests(guestId, new H_GrenadeThrowT
        {
            PlayerId = guestId,
            SkillId  = packet.SkillId,
            Origin   = originRaw.HasValue ? originRaw.Value.UnPack() : new Vec3T(),
            Target   = targetRaw.HasValue ? targetRaw.Value.UnPack() : new Vec3T(),
        }, H_GrenadeThrow.Pack, PacketType.H_GrenadeThrow);
    }
}
