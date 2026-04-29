using System;
using Google.FlatBuffers;

public static class PacketBuilder
{
    public static ArraySegment<byte> Build(FlatBufferBuilder builder, PacketType type, int innerOffset)
    {
        Offset<FlatPacket> rootOffset = FlatPacket.CreateFlatPacket(builder, type, innerOffset);
        FlatPacket.FinishFlatPacketBuffer(builder, rootOffset);

        byte[] payload = builder.SizedByteArray();

        int totalSize = payload.Length + Session.HeaderSize;
        if (totalSize > ushort.MaxValue)
            throw new InvalidOperationException($"Packet too large: {totalSize} bytes (type={type}).");

        byte[] sendBuffer = new byte[totalSize];
        BitConverter.GetBytes((ushort)totalSize).CopyTo(sendBuffer, 0);
        Buffer.BlockCopy(payload, 0, sendBuffer, Session.HeaderSize, payload.Length);

        return new ArraySegment<byte>(sendBuffer);
    }

    public static void Send(Session session, FlatBufferBuilder builder, PacketType type, int innerOffset)
    {
        session.Send(Build(builder, type, innerOffset));
    }
}
