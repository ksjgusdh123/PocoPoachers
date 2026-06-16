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

    public static void Equip(int itemId, int slotIndex)
    {
        if (IsSolo) return;

        int id = MyId;

        if (RoomManager.IsHost)
            PacketBuilder.BroadcastToGuests(
                new H_EquipT { PlayerId = id, ItemId = itemId, SlotIndex = slotIndex },
                H_Equip.Pack, PacketType.H_Equip);
        else
            PacketBuilder.SendToHost(
                new G_EquipT { PlayerId = id, ItemId = itemId, SlotIndex = slotIndex },
                G_Equip.Pack, PacketType.G_Equip);
    }

    public static void ItemGain(bool isPlayerGained, int boxUid, int itemTypeId, int amount, int addedSlotIndex, int removedSlotIndex)
    {
        if (IsSolo) return;
        PacketBuilder.SendToHost(new G_ItemGainT
        {
            IsPlayerGained   = isPlayerGained,
            BoxUid           = boxUid,
            ItemTypeId       = itemTypeId,
            Amount           = amount,
            AddedSlotIndex   = addedSlotIndex,
            RemovedSlotIndex = removedSlotIndex,
        }, G_ItemGain.Pack, PacketType.G_ItemGain);
    }

    public static void ItemExchange(int boxUid, int playerItemId, int playerItemAmount, int playerSlotIndex, int boxItemId, int boxItemAmount, int boxSlotIndex)
    {
        if (IsSolo) return;
        PacketBuilder.SendToHost(new G_ItemExchangeT
        {
            BoxUid           = boxUid,
            PlayerItemId     = playerItemId,
            PlayerItemAmount = playerItemAmount,
            PlayerSlotIndex  = playerSlotIndex,
            BoxItemId        = boxItemId,
            BoxItemAmount    = boxItemAmount,
            BoxSlotIndex     = boxSlotIndex,
        }, G_ItemExchange.Pack, PacketType.G_ItemExchange);
    }

    public static void ItemBoxUpdate(int boxUid, int itemTypeId, int amount, int slotIndex)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(new H_ItemBoxUpdateT
        {
            BoxUid     = boxUid,
            ItemTypeId = itemTypeId,
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

    public static void EnemySpawnToGuest(int guestPlayerId, int enemyTypeId, int enemyId, Vector3 pos, float rotation, float hp, float maxHp, int weaponId, int helmetId)
    {
        PacketBuilder.SendToGuest(guestPlayerId, new H_EnemySpawnT
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

    public static void EnemyMove(int enemyId, Vector3 pos, float rotation)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(new H_EnemyMoveT
        {
            EnemyId  = enemyId,
            Pos      = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z },
            Rotation = rotation,
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

    public static void ItemSpawn(int uid, int typeId, Vector3 pos, float rotation, List<int> itemIds)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(new H_ItemSpawnT
        {
            Uid     = uid,
            TypeId  = typeId,
            Pos     = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z },
            Rotation = rotation,
            ItemIds = itemIds,
        }, H_ItemSpawn.Pack, PacketType.H_ItemSpawn);
    }
}
