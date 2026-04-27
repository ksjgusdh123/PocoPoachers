using System;
using System.Collections.Generic;
using Google.FlatBuffers;
using UnityEngine;

public static class PacketManager
{
    static readonly Dictionary<PacketType, Action<FlatPacket>> _onRecv = new Dictionary<PacketType, Action<FlatPacket>>();

    static PacketManager()
    {
        Register();
    }

    static void Register()
    {
        _onRecv.Add(PacketType.S_LoginRes,      PacketHandlers.OnS_LoginRes);
        _onRecv.Add(PacketType.S_MoveNtf,       PacketHandlers.OnS_MoveNtf);
        _onRecv.Add(PacketType.S_WorldItemSpawnNtf,   PacketHandlers.OnS_WorldItemSpawnNtf);
        _onRecv.Add(PacketType.S_WorldItemDespawnNtf, PacketHandlers.OnS_WorldItemDespawnNtf);
        _onRecv.Add(PacketType.S_SpawnItemBoxNtf, PacketHandlers.OnS_SpawnItemBoxNtf);
        _onRecv.Add(PacketType.S_AddItemRes,    PacketHandlers.OnS_AddItemRes);
        _onRecv.Add(PacketType.S_RemoveItemRes, PacketHandlers.OnS_RemoveItemRes);
        _onRecv.Add(PacketType.S_InventoryNtf,  PacketHandlers.OnS_InventoryNtf);
        _onRecv.Add(PacketType.S_ChangeItemBox,  PacketHandlers.OnS_ChangeItemBox);
        _onRecv.Add(PacketType.S_SuccessGainItemNtf,  PacketHandlers.OnS_SuccessGainItemNtf);
    }

    public static void HandlePacket(ArraySegment<byte> buffer)
    {
        if (buffer.Array == null || buffer.Count <= Session.HeaderSize)
            return;

        int bodyOffset = buffer.Offset + Session.HeaderSize;
        var bb = new ByteBuffer(buffer.Array, bodyOffset);
        var root = FlatPacket.GetRootAsFlatPacket(bb);

        PacketType type = root.TypeType;
        int bodyLen = buffer.Count - Session.HeaderSize;
        if (type != PacketType.S_MoveNtf)
            Debug.Log($"[Packet] recv {type} body={bodyLen}b");

        if (!_onRecv.TryGetValue(type, out Action<FlatPacket> action))
        {
            Debug.LogWarning($"[PacketManager] Unknown packet type: {type}");
            return;
        }

        try
        {
            action.Invoke(root);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"[PacketManager] Handler failed: {type}");
        }
    }
}
