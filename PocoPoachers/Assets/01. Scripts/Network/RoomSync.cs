using System.Collections.Generic;
using UnityEngine;

public static class RoomSync
{
    private static int MyId => NetworkManager.Instance?.MyPlayerId ?? 0;
    private static bool IsSolo => RoomManager.IsHost && !RoomManager.HasGuests;

    public static void Move(Vector3 pos, float yaw, sbyte moveType, float velX, float velZ, bool isSprinting, bool isRolling, bool isAiming = false, bool isReloading = false)
    {
        if (IsSolo) return;

        var vec = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z };
        int id = MyId;

        if (RoomManager.IsHost)
            PacketBuilder.BroadcastToGuests(
                new H_MoveT { PlayerId = id, Pos = vec, Rotation = yaw, MoveType = moveType, VelocityX = velX, VelocityZ = velZ, IsSprinting = isSprinting, IsRolling = isRolling, IsAiming = isAiming, IsReloading = isReloading },
                H_Move.Pack, PacketType.H_Move);
        else
            PacketBuilder.SendToHost(
                new G_MoveT { PlayerId = id, Pos = vec, Rotation = yaw, MoveType = moveType, VelocityX = velX, VelocityZ = velZ, IsSprinting = isSprinting, IsRolling = isRolling, IsAiming = isAiming, IsReloading = isReloading },
                G_Move.Pack, PacketType.G_Move);
    }

    public static void Shoot(Vector3 origin, Vector3 direction, GunStatData stat)
    {
        if (IsSolo) return;

        var originT = new Vec3T { X = origin.x, Y = origin.y, Z = origin.z };
        var dirT    = new Vec3T { X = direction.x, Y = direction.y, Z = direction.z };
        int id = MyId;

        if (RoomManager.IsHost)
            PacketBuilder.BroadcastToGuests(
                new H_ShootT { PlayerId = id, Origin = originT, Direction = dirT, BulletSpeed = stat.BulletSpeed, Damage = stat.Damage, MaxRange = stat.BulletRange },
                H_Shoot.Pack, PacketType.H_Shoot);
        else
            PacketBuilder.SendToHost(
                new G_ShootT { PlayerId = id, Origin = originT, Direction = dirT, BulletSpeed = stat.BulletSpeed, Damage = stat.Damage, MaxRange = stat.BulletRange, SoundRange = stat.SoundRange },
                G_Shoot.Pack, PacketType.G_Shoot);
    }

    public static void Equip(int itemId, int slotIndex, int itemUid = 0)
    {
        if (IsSolo) return;

        int id = MyId;

        if (RoomManager.IsHost)
            PacketBuilder.BroadcastToGuests(
                new H_EquipT { PlayerId = id, ItemId = itemId, ItemUid = itemUid, SlotIndex = slotIndex },
                H_Equip.Pack, PacketType.H_Equip);
        else
            PacketBuilder.SendReliableToHost(
                new G_EquipT { PlayerId = id, ItemId = itemId, ItemUid = itemUid, SlotIndex = slotIndex },
                G_Equip.Pack, PacketType.G_Equip);
    }

    // 장비 내구도 변화(발사/피격 등) — 호스트가 실제 값을 계산해서 결과를 브로드캐스트
    public static void Durability(int itemUid, int itemId, float amount, float defaultMaxDurability)
    {
        if (itemUid == 0) return; // uid가 없는(추적되지 않는) 아이템은 동기화하지 않음

        if (RoomManager.IsHost)
        {
            var (current, max) = WorldEquipmentManager.ApplyChange(itemUid, itemId, amount, defaultMaxDurability);
            if (RoomManager.HasGuests)
                PacketBuilder.BroadcastToGuests(new H_DurabilityT { ItemUid = itemUid, Current = current, Max = max },
                    H_Durability.Pack, PacketType.H_Durability);
        }
        else
        {
            PacketBuilder.SendReliableToHost(new G_DurabilityT { ItemUid = itemUid, ItemId = itemId, Amount = amount },
                G_Durability.Pack, PacketType.G_Durability);
        }
    }

    // 무기 해제 시점의 탄약 저장 — 호스트는 직접 저장하고, 게스트는 호스트에게 요청한다
    public static void GunAmmoSave(int gunUid, int currentAmmo, int maxMagazine)
    {
        if (gunUid == 0) return;

        if (RoomManager.IsHost)
        {
            Debug.Log($"[AmmoSave] 저장 uid={gunUid} ammo={currentAmmo}/{maxMagazine}"); // TODO: 디버그 후 제거
            WorldEquipmentManager.SetAmmo(gunUid, currentAmmo, maxMagazine);
        }
        else
        {
            PacketBuilder.SendReliableToHost(new G_GunAmmoSaveT
            {
                GunUid      = gunUid,
                CurrentAmmo = currentAmmo,
                MaxMagazine = maxMagazine,
            }, G_GunAmmoSave.Pack, PacketType.G_GunAmmoSave);
        }
    }

    // 총 파츠 장착/해제 — partId=0이면 해제. 호스트는 직접 저장하고, 게스트는 호스트에게 요청한다
    // currentAmmo/maxMagazine은 파츠 변경 직후 호출자(자기 자신) 총의 실제 값을 그대로 전달한다
    public static void GunPartEquip(int gunUid, SlotType slotType, int partId, int partUid, int currentAmmo, int maxMagazine)
    {
        if (gunUid == 0) return;

        if (RoomManager.IsHost)
        {
            if (partId != 0) WorldEquipmentManager.SetPart(gunUid, slotType, partId, partUid);
            else WorldEquipmentManager.RemovePart(gunUid, slotType);
            WorldEquipmentManager.SetAmmo(gunUid, currentAmmo, maxMagazine);
        }
        else
        {
            PacketBuilder.SendReliableToHost(new G_GunPartEquipT
            {
                GunUid      = gunUid,
                SlotType    = (int)slotType,
                PartId      = partId,
                PartUid     = partUid,
                CurrentAmmo = currentAmmo,
                MaxMagazine = maxMagazine,
            }, G_GunPartEquip.Pack, PacketType.G_GunPartEquip);
        }
    }

    public static void ItemGain(bool isPlayerGained, int boxUid, int itemTypeId, int itemUid, int amount, int addedSlotIndex, int removedSlotIndex)
    {
        if (IsSolo) return;
        PacketBuilder.SendReliableToHost(new G_ItemGainT
        {
            IsPlayerGained   = isPlayerGained,
            BoxUid           = boxUid,
            ItemTypeId       = itemTypeId,
            ItemUid          = itemUid,
            Amount           = amount,
            AddedSlotIndex   = addedSlotIndex,
            RemovedSlotIndex = removedSlotIndex,
        }, G_ItemGain.Pack, PacketType.G_ItemGain);
    }

    public static void ItemExchange(int boxUid, int playerItemId, int playerItemAmount, int playerItemUid, int playerSlotIndex, int boxItemId, int boxItemAmount, int boxItemUid, int boxSlotIndex)
    {
        if (IsSolo) return;
        PacketBuilder.SendReliableToHost(new G_ItemExchangeT
        {
            BoxUid           = boxUid,
            PlayerItemId     = playerItemId,
            PlayerItemAmount = playerItemAmount,
            PlayerItemUid    = playerItemUid,
            PlayerSlotIndex  = playerSlotIndex,
            BoxItemId        = boxItemId,
            BoxItemAmount    = boxItemAmount,
            BoxItemUid       = boxItemUid,
            BoxSlotIndex     = boxSlotIndex,
        }, G_ItemExchange.Pack, PacketType.G_ItemExchange);
    }

    public static void ItemBoxUpdate(int boxUid, int itemTypeId, int amount, int slotIndex, int itemUid = 0)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(new H_ItemBoxUpdateT
        {
            BoxUid     = boxUid,
            ItemTypeId = itemTypeId,
            ItemUid    = itemUid,
            Amount     = amount,
            SlotIndex  = slotIndex,
        }, H_ItemBoxUpdate.Pack, PacketType.H_ItemBoxUpdate);
    }

    public static void StatSync(float hp, float maxHp, float stamina = 0f, float battery = 0f, float defense = 0f)
    {
        if (IsSolo) return;

        if (RoomManager.IsHost)
            PacketBuilder.BroadcastToGuests(new H_StatSyncT
            {
                PlayerId = MyId,
                Hp       = hp,
                MaxHp    = maxHp,
                Stamina  = stamina,
                Battery  = battery,
                Defense  = defense,
            }, H_StatSync.Pack, PacketType.H_StatSync);
        else
            PacketBuilder.SendToHost(new G_StatSyncT
            {
                Hp      = hp,
                MaxHp   = maxHp,
                Stamina = stamina,
                Battery = battery,
                Defense = defense,
            }, G_StatSync.Pack, PacketType.G_StatSync);
    }

    public static void Leave()
    {
        int id = MyId;
        if (RoomManager.IsHost)
            PacketBuilder.BroadcastToGuests(
                new H_LeaveT { PlayerId = id, IsHost = true },
                H_Leave.Pack, PacketType.H_Leave);
        else
            PacketBuilder.SendToHost(
                new G_LeaveT { PlayerId = id },
                G_Leave.Pack, PacketType.G_Leave);
    }

    public static void ShelterLevel(int level)
    {
        if (IsSolo) return;

        if (RoomManager.IsHost)
            PacketBuilder.BroadcastToGuests(
                new H_ShelterLevelT { Level = level },
                H_ShelterLevel.Pack, PacketType.H_ShelterLevel);
        else
            PacketBuilder.SendReliableToHost(
                new G_ShelterLevelT { Level = level },
                G_ShelterLevel.Pack, PacketType.G_ShelterLevel);
    }

    // 게스트가 씬 로드 완료를 호스트에 알림 (호스트가 박스/적 스냅샷을 보내는 트리거)
    public static void SceneReady()
    {
        if (RoomManager.IsHost) return;

        PacketBuilder.SendReliableToHost(
            new G_SceneReadyT { PlayerId = MyId },
            G_SceneReady.Pack, PacketType.G_SceneReady);
    }

    public static void EnemySpawnToGuest(int guestPlayerId, int enemyTypeId, int enemyId, Vector3 pos, float rotation, float hp, float maxHp, int weaponId, int helmetId)
    {
        PacketBuilder.SendReliableToGuest(guestPlayerId, new H_EnemySpawnT
        {
            EnemyTypeId = enemyTypeId,
            EnemyId  = enemyId,
            Pos      = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z },
            Rotation = rotation,
            Hp       = hp,
            MaxHp    = maxHp,
            WeaponId = weaponId,
            HelmetId = helmetId,
        }, H_EnemySpawn.Pack, PacketType.H_EnemySpawn);
    }

    public static void EnemyMove(int enemyId, Vector3 pos, float rotation, int animState)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(new H_EnemyMoveT
        {
            EnemyId   = enemyId,
            Pos       = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z },
            Rotation  = rotation,
            AnimState = animState,
        }, H_EnemyMove.Pack, PacketType.H_EnemyMove);
    }

    public static void EnemyHit(int enemyId, float hp, float maxHp, float damage)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(new H_EnemyHitT
        {
            EnemyId = enemyId,
            Hp      = hp,
            MaxHp   = maxHp,
            Damage = damage,
        }, H_EnemyHit.Pack, PacketType.H_EnemyHit);
    }

    public static void EnemyDie(int enemyId)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(new H_EnemyDieT
        {
            EnemyId = enemyId,
        }, H_EnemyDie.Pack, PacketType.H_EnemyDie);
    }

    public static void ItemSpawn(int uid, int typeId, Vector3 pos, float rotation, List<int> itemIds, List<int> itemCounts = null, List<int> itemUids = null)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(new H_ItemSpawnT
        {
            Uid     = uid,
            TypeId  = typeId,
            Pos     = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z },
            Rotation = rotation,
            ItemIds = itemIds,
            ItemCount = itemCounts,
            ItemUids = itemUids,
        }, H_ItemSpawn.Pack, PacketType.H_ItemSpawn);
    }
}
