using Google.FlatBuffers;
using System;
using System.Collections.Generic;

namespace Server;

public static class PacketSender
{
    const int BufferSize = 1024;

    public static void SLoginRes(ClientSession session, bool success, int userId, string userName, int level)
    {
        var fb = new FlatBufferBuilder(BufferSize);
        var nameOff = fb.CreateString(success ? userName : string.Empty);
        var userInfoOff = UserInfo.CreateUserInfo(fb, userId, nameOff, level);
        var bodyOff = S_LoginRes.CreateS_LoginRes(fb, success, userInfoOff);
        PacketBuilder.Send(session, fb, PacketType.S_LoginRes, bodyOff.Value);
    }

    public static void SInventoryNtf(ClientSession session, Dictionary<int, int> items)
    {
        var fb = new FlatBufferBuilder(BufferSize);
        var itemOffsets = new Offset<InventoryItem>[items.Count];
        int i = 0;
        foreach (var kv in items)
            itemOffsets[i++] = InventoryItem.CreateInventoryItem(fb, kv.Key, kv.Value);
        var vecOff = S_InventoryNtf.CreateItemsVector(fb, itemOffsets);
        var ntfOff = S_InventoryNtf.CreateS_InventoryNtf(fb, vecOff);
        PacketBuilder.Send(session, fb, PacketType.S_InventoryNtf, ntfOff.Value);
    }

    public static void SMoveNtfBroadcast(ClientSession? except, int playerId, float x, float y, float z, float rotation, sbyte moveType)
    {
        var fb = new FlatBufferBuilder(BufferSize);
        S_MoveNtf.StartS_MoveNtf(fb);
        S_MoveNtf.AddPlayerId(fb, playerId);
        S_MoveNtf.AddPos(fb, Vec3.CreateVec3(fb, x, y, z));
        S_MoveNtf.AddRotation(fb, rotation);
        S_MoveNtf.AddMoveType(fb, moveType);
        Offset<S_MoveNtf> bodyOff = S_MoveNtf.EndS_MoveNtf(fb);
        SessionManager.Instance.Broadcast(fb, PacketType.S_MoveNtf, bodyOff.Value, except);
    }

    public static void SShootNtfBroadcast(
        ClientSession? except,
        int playerId,
        float ox, float oy, float oz,
        float dx, float dy, float dz,
        float bulletSpeed,
        float damage,
        float maxRange)
    {
        var fb = new FlatBufferBuilder(BufferSize);
        S_ShootNtf.StartS_ShootNtf(fb);
        S_ShootNtf.AddPlayerId(fb, playerId);
        S_ShootNtf.AddOrigin(fb, Vec3.CreateVec3(fb, ox, oy, oz));
        S_ShootNtf.AddDirection(fb, Vec3.CreateVec3(fb, dx, dy, dz));
        S_ShootNtf.AddBulletSpeed(fb, bulletSpeed);
        S_ShootNtf.AddDamage(fb, damage);
        S_ShootNtf.AddMaxRange(fb, maxRange);
        Offset<S_ShootNtf> bodyOff = S_ShootNtf.EndS_ShootNtf(fb);
        SessionManager.Instance.Broadcast(fb, PacketType.S_ShootNtf, bodyOff.Value, except);
    }

    public static void SSpawnItemBoxNtfBroadcast(int uid, int typeId, float x, float y, float z, float rotation, int[] itemIds)
    {
        var fb = new FlatBufferBuilder(BufferSize);
        var itemVec = S_SpawnItemBoxNtf.CreateItemIdsVector(fb, itemIds);
        S_SpawnItemBoxNtf.StartS_SpawnItemBoxNtf(fb);
        S_SpawnItemBoxNtf.AddUid(fb, uid);
        S_SpawnItemBoxNtf.AddTypeId(fb, typeId);
        S_SpawnItemBoxNtf.AddPos(fb, Vec3.CreateVec3(fb, x, y, z));
        S_SpawnItemBoxNtf.AddRotation(fb, rotation);
        S_SpawnItemBoxNtf.AddItemIds(fb, itemVec);
        var bodyOff = S_SpawnItemBoxNtf.EndS_SpawnItemBoxNtf(fb);
        PacketBuilder.Broadcast(SessionManager.Instance.Snapshot(), fb, PacketType.S_SpawnItemBoxNtf, bodyOff.Value);
    }

    public static void SChangeItemBox(ClientSession session, bool isGain, int boxUid, int typeId, int removedAmount)
    {
        var fb = new FlatBufferBuilder(BufferSize);
        var bodyOff = S_ChangeItemBox.CreateS_ChangeItemBox(fb, isGain, boxUid, typeId, removedAmount);
        SessionManager.Instance.Broadcast(fb, PacketType.S_ChangeItemBox, bodyOff.Value, session);
    }

    public static void SSuccessGainItemNtf(ClientSession session, int uid, int typeId, int amount)
    {
        var fb = new FlatBufferBuilder(BufferSize);
        var bodyOff = S_SuccessGainItemNtf.CreateS_SuccessGainItemNtf(fb, uid, typeId, amount);
        PacketBuilder.Send(session, fb, PacketType.S_SuccessGainItemNtf, bodyOff.Value);
    }

    public static void SExchangeItemResultNtf(ClientSession session, bool isSuccess, int boxUid, int playerItemId, int playerItemAmont,
        int boxItemId, int boxItemAmount)
    {
        var fb = new FlatBufferBuilder(BufferSize);
        var bodyOff = S_ExchangeItemResultNtf.CreateS_ExchangeItemResultNtf(fb, isSuccess, boxUid, boxItemId, 
            boxItemAmount, playerItemId, playerItemAmont);
        PacketBuilder.Send(session, fb, PacketType.S_ExchangeItemResultNtf, bodyOff.Value);
    }

    public static void SWorldItemDespawnNtfBroadcast(int uid)
    {
        var fb = new FlatBufferBuilder(BufferSize);
        var bodyOff = S_WorldItemDespawnNtf.CreateS_WorldItemDespawnNtf(fb, uid);
        PacketBuilder.Broadcast(SessionManager.Instance.Snapshot(), fb, PacketType.S_WorldItemDespawnNtf, bodyOff.Value);
    }

}
