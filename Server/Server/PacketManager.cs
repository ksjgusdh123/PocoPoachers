using Google.FlatBuffers;
using ServerCore;

namespace Server;

public class PacketManager
{
    static Dictionary<PacketType, Action<ClientSession, FlatPacket>> _onRecv = new();
    static PacketHandler _handler = new PacketHandler();

    static PacketManager()
    {
        Register();
    }

    private static void Register()
    {
        _onRecv.Add(PacketType.C_LoginReq, (session, root) => _handler.OnC_LoginReq(session, root.TypeAsC_LoginReq()));
        _onRecv.Add(PacketType.C_MoveReq, (session, root) => _handler.OnC_MoveReq(session, root.TypeAsC_MoveReq()));
    }

    public static void HandlePacket(ClientSession session, ArraySegment<byte> buffer)
    {
        if (buffer.Array == null || buffer.Count <= PacketSession.HeaderSize)
            return;

        int bodyOffset = buffer.Offset + PacketSession.HeaderSize;

        var bb = new ByteBuffer(buffer.Array, bodyOffset);
        var root = FlatPacket.GetRootAsFlatPacket(bb);

        if (_onRecv.TryGetValue(root.TypeType, out var action))
        {
            action.Invoke(session, root);
        }
    }
}
