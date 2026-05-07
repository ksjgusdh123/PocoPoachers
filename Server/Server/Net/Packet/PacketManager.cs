using System;
using Google.FlatBuffers;
using ServerCore;

namespace Server;

public static partial class PacketManager
{
    static readonly Dictionary<PacketType, Action<ClientSession, FlatPacket>> _onRecv = new();
    static readonly PacketHandler _handler = new();

    static PacketManager() { Register(); }

    public static void HandlePacket(ClientSession session, ArraySegment<byte> buffer)
    {
        if (buffer.Array == null || buffer.Count <= PacketSession.HeaderSize)
            return;

        int bodyOffset = buffer.Offset + PacketSession.HeaderSize;
        var bb = new ByteBuffer(buffer.Array, bodyOffset);
        var root = FlatPacket.GetRootAsFlatPacket(bb);

        PacketType type = root.TypeType;
        if (type != PacketType.C_Heartbeat)
            LOG($"Recv {type}");

        if (!_onRecv.TryGetValue(type, out var action))
        {
            LOG_W($"[PacketManager] Unknown packet type: {type}");
            return;
        }

        try { action.Invoke(session, root); }
        catch (Exception e) { LOG_E($"[PacketManager] Handler failed: {type} — {e}"); }
    }
}
