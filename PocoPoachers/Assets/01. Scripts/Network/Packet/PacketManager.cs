using System;
using System.Collections.Generic;
using Google.FlatBuffers;
using UnityEngine;

public static partial class PacketManager
{
    static readonly Dictionary<PacketType, Action<FlatPacket>> _onRecv = new Dictionary<PacketType, Action<FlatPacket>>();

    static PacketManager() { Register(); }

    public static void HandlePacket(ArraySegment<byte> buffer)
    {
        if (buffer.Array == null || buffer.Count <= Session.HeaderSize)
            return;

        int bodyOffset = buffer.Offset + Session.HeaderSize;
        var bb = new ByteBuffer(buffer.Array, bodyOffset);
        var root = FlatPacket.GetRootAsFlatPacket(bb);

        PacketType type = root.TypeType;
        Debug.Log($"[PacketManager] Recv {type}");

        if (!_onRecv.TryGetValue(type, out Action<FlatPacket> action))
        {
            Debug.LogWarning($"[PacketManager] Unknown packet type: {type}");
            return;
        }

        try { action.Invoke(root); }
        catch (Exception e)
        {
            Debug.LogException(e);
            Debug.LogError($"[PacketManager] Handler failed: {type}");
        }
    }
}
