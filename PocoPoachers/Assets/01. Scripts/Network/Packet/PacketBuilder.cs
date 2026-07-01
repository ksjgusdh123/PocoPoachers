using System;
using Google.FlatBuffers;

public static class PacketBuilder
{
    static readonly FlatBufferBuilder _builder = new FlatBufferBuilder(1024);

    public static void SendToMaster<TTable, TObj>(TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> pack, PacketType type)
        where TTable : struct where TObj : class
    {
        var session = NetworkManager.Instance?.Session;
        if (session == null) return;

        _builder.Clear();
        var innerOffset = pack(_builder, data);
        session.Send(Build(_builder, type, innerOffset.Value));
    }

    public static void SendToHost<TTable, TObj>(TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> pack, PacketType type)
        where TTable : struct where TObj : class
        => RoomManager.Instance?.UdpSendToHost(data, pack, type);

    public static void BroadcastToGuests<TTable, TObj>(int skipPlayerId, TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> pack, PacketType type)
        where TTable : struct where TObj : class
        => RoomManager.Instance?.UdpBroadcastToGuests(data, pack, type, skipPlayerId);

    public static void BroadcastToGuests<TTable, TObj>(TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> pack, PacketType type)
        where TTable : struct where TObj : class
        => RoomManager.Instance?.UdpBroadcastToGuests(data, pack, type);

    public static void SendToGuest<TTable, TObj>(int playerId, TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> pack, PacketType type)
        where TTable : struct where TObj : class
        => RoomManager.Instance?.UdpSendToGuest(playerId, data, pack, type);

    public static void SendReliableToGuest<TTable, TObj>(int playerId, TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> pack, PacketType type)
        where TTable : struct where TObj : class
        => RoomManager.Instance?.UdpSendReliableToGuest(playerId, data, pack, type);

    public static void BroadcastReliableToGuests<TTable, TObj>(int skipPlayerId, TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> pack, PacketType type)
        where TTable : struct where TObj : class
        => RoomManager.Instance?.UdpBroadcastReliableToGuests(data, pack, type, skipPlayerId);

    public static void BroadcastReliableToGuests<TTable, TObj>(TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> pack, PacketType type)
        where TTable : struct where TObj : class
        => RoomManager.Instance?.UdpBroadcastReliableToGuests(data, pack, type);

    public static ArraySegment<byte> BuildSegment<TTable, TObj>(TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> pack, PacketType type)
        where TTable : struct where TObj : class
    {
        _builder.Clear();
        var innerOffset = pack(_builder, data);
        return Build(_builder, type, innerOffset.Value);
    }

    static ArraySegment<byte> Build(FlatBufferBuilder builder, PacketType type, int innerOffset)
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
}
