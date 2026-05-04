using Google.FlatBuffers;
using ServerCore;

namespace Server;

public static partial class PacketBuilder
{
    private const int BufferSize = 1024;
    private static ArraySegment<byte> BuildInternal<TTable, TObj>(TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> packFunc, PacketType type)
        where TTable : struct
        where TObj : class
    {
        var builder = new FlatBufferBuilder(BufferSize);
        var innerOffset = packFunc(builder, data);
        var rootOffset = FlatPacket.CreateFlatPacket(builder, type, innerOffset.Value);
        FlatPacket.FinishFlatPacketBuffer(builder, rootOffset);

        byte[] payload = builder.SizedByteArray();
        int totalSize = payload.Length + PacketSession.HeaderSize;
        byte[] sendBuffer = new byte[totalSize];

        BitConverter.GetBytes((ushort)totalSize).CopyTo(sendBuffer, 0);
        Buffer.BlockCopy(payload, 0, sendBuffer, PacketSession.HeaderSize, payload.Length);

        return new ArraySegment<byte>(sendBuffer);
    }

    public static void Send<TTable, TObj>(ClientSession session, TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> packFunc, PacketType type)
        where TTable : struct
        where TObj : class
    {
        session.Send(BuildInternal(data, packFunc, type));
    }

    public static void Broadcast<TTable, TObj>(IEnumerable<ClientSession> sessions, TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> packFunc, PacketType type)
        where TTable : struct
        where TObj : class
    {
        var segment = BuildInternal(data, packFunc, type);
        foreach (var session in sessions) session.Send(segment);
    }
}