using UnityEngine;

public static class RoomSync
{
    private static int MyId => NetworkManager.Instance?.MyPlayerId ?? 0;
    private static bool IsSolo => RoomManager.IsHost && !RoomManager.HasGuests;

    public static void Move(Vector3 pos, float yaw, sbyte moveType)
    {
        if (IsSolo) return;

        var vec = new Vec3T { X = pos.x, Y = pos.y, Z = pos.z };
        int id = MyId;

        if (RoomManager.IsHost)
            PacketBuilder.BroadcastToGuests(
                new H_MoveT { PlayerId = id, Pos = vec, Rotation = yaw, MoveType = moveType },
                H_Move.Pack, PacketType.H_Move);
        else
            PacketBuilder.SendToHost(
                new G_MoveT { PlayerId = id, Pos = vec, Rotation = yaw, MoveType = moveType },
                G_Move.Pack, PacketType.G_Move);
    }

    public static void Shoot(Vector3 origin, Vector3 direction, GunData gunData)
    {
        if (IsSolo) return;

        var originT = new Vec3T { X = origin.x, Y = origin.y, Z = origin.z };
        var dirT    = new Vec3T { X = direction.x, Y = direction.y, Z = direction.z };
        int id = MyId;

        if (RoomManager.IsHost)
            PacketBuilder.BroadcastToGuests(
                new H_ShootT { PlayerId = id, Origin = originT, Direction = dirT, BulletSpeed = gunData.bulletSpeed, Damage = gunData.damage, MaxRange = gunData.range },
                H_Shoot.Pack, PacketType.H_Shoot);
        else
            PacketBuilder.SendToHost(
                new G_ShootT { PlayerId = id, Origin = originT, Direction = dirT, BulletSpeed = gunData.bulletSpeed, Damage = gunData.damage, MaxRange = gunData.range },
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

    public static void ItemGain(G_ItemGainT data)
    {
        if (IsSolo) return;
        PacketBuilder.SendToHost(data, G_ItemGain.Pack, PacketType.G_ItemGain);
    }

    public static void ItemExchange(G_ItemExchangeT data)
    {
        if (IsSolo) return;
        PacketBuilder.SendToHost(data, G_ItemExchange.Pack, PacketType.G_ItemExchange);
    }

    public static void ItemBoxUpdate(H_ItemBoxUpdateT data)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(data, H_ItemBoxUpdate.Pack, PacketType.H_ItemBoxUpdate);
    }

    public static void ItemSpawn(H_ItemSpawnT data)
    {
        if (!RoomManager.HasGuests) return;
        PacketBuilder.BroadcastToGuests(data, H_ItemSpawn.Pack, PacketType.H_ItemSpawn);
    }
}
