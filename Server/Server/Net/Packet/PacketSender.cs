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

    public static void SSpawnItemBoxNtfBroadcast(int typeId, float x, float y, float z, float rotation, int[] itemIds)
    {
        var fb = new FlatBufferBuilder(BufferSize);
        var itemVec = S_SpawnItemBoxNtf.CreateItemIdsVector(fb, itemIds);
        S_SpawnItemBoxNtf.StartS_SpawnItemBoxNtf(fb);
        S_SpawnItemBoxNtf.AddTypeId(fb, typeId);
        S_SpawnItemBoxNtf.AddPos(fb, Vec3.CreateVec3(fb, x, y, z));
        S_SpawnItemBoxNtf.AddRotation(fb, rotation);
        S_SpawnItemBoxNtf.AddItemIds(fb, itemVec);
        var bodyOff = S_SpawnItemBoxNtf.EndS_SpawnItemBoxNtf(fb);
        PacketBuilder.Broadcast(SessionManager.Instance.Snapshot(), fb, PacketType.S_SpawnItemBoxNtf, bodyOff.Value);
    }

    public static void SAddItemRes(ClientSession session, bool success, int itemId, int amount)
    {
        var fb = new FlatBufferBuilder(BufferSize);
        var bodyOff = S_AddItemRes.CreateS_AddItemRes(fb, success, itemId, amount);
        PacketBuilder.Send(session, fb, PacketType.S_AddItemRes, bodyOff.Value);
    }

    public static void SRemoveItemRes(ClientSession session, bool success, int itemId, int amount)
    {
        var fb = new FlatBufferBuilder(BufferSize);
        var bodyOff = S_RemoveItemRes.CreateS_RemoveItemRes(fb, success, itemId, amount);
        PacketBuilder.Send(session, fb, PacketType.S_RemoveItemRes, bodyOff.Value);
    }

    public static void SWorldItemSpawnNtf(ClientSession session, int uid, int typeId, float x, float y, float z, float rotation)
    {
        var fb = new FlatBufferBuilder(BufferSize);
        var bodyOff = BuildWorldItemSpawn(fb, uid, typeId, x, y, z, rotation);
        PacketBuilder.Send(session, fb, PacketType.S_WorldItemSpawnNtf, bodyOff.Value);
    }

    public static void SWorldItemSpawnNtfBroadcast(int uid, int typeId, float x, float y, float z, float rotation)
    {
        var fb = new FlatBufferBuilder(BufferSize);
        var bodyOff = BuildWorldItemSpawn(fb, uid, typeId, x, y, z, rotation);
        PacketBuilder.Broadcast(SessionManager.Instance.Snapshot(), fb, PacketType.S_WorldItemSpawnNtf, bodyOff.Value);
    }

    public static void SWorldItemDespawnNtfBroadcast(int uid)
    {
        var fb = new FlatBufferBuilder(BufferSize);
        var bodyOff = S_WorldItemDespawnNtf.CreateS_WorldItemDespawnNtf(fb, uid);
        PacketBuilder.Broadcast(SessionManager.Instance.Snapshot(), fb, PacketType.S_WorldItemDespawnNtf, bodyOff.Value);
    }

    static Offset<S_WorldItemSpawnNtf> BuildWorldItemSpawn(FlatBufferBuilder fb, int uid, int typeId, float x, float y, float z, float rotation)
    {
        S_WorldItemSpawnNtf.StartS_WorldItemSpawnNtf(fb);
        S_WorldItemSpawnNtf.AddUid(fb, uid);
        S_WorldItemSpawnNtf.AddTypeId(fb, typeId);
        S_WorldItemSpawnNtf.AddPos(fb, Vec3.CreateVec3(fb, x, y, z));
        S_WorldItemSpawnNtf.AddRotation(fb, rotation);
        return S_WorldItemSpawnNtf.EndS_WorldItemSpawnNtf(fb);
    }
}
