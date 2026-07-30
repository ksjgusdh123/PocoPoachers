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

    public static void SendReliableToHost<TTable, TObj>(TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> pack, PacketType type)
        where TTable : struct where TObj : class
        => RoomManager.Instance?.UdpSendReliableToHost(data, pack, type);

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

        // SizedByteArray()는 완성된 페이로드를 새 배열로 한 번 더 복사한다.
        // DataBuffer를 직접 참조하면 그 복사를 없애고 전송 버퍼로의 복사 1회만 남는다.
        // (Move 등 주기 전송 패킷에서 패킷당 할당 2회 → 1회)
        var dataBuffer = builder.DataBuffer;
        int payloadStart = dataBuffer.Position;
        int payloadLen   = dataBuffer.Length - payloadStart;

        int totalSize = payloadLen + Session.HeaderSize;
        if (totalSize > ushort.MaxValue)
            throw new InvalidOperationException($"Packet too large: {totalSize} bytes (type={type}).");

        byte[] sendBuffer = new byte[totalSize];

        // BitConverter.GetBytes는 byte[2]를 새로 할당한다 — 직접 기록해 동일 바이트를 만든다.
        if (BitConverter.IsLittleEndian)
        {
            sendBuffer[0] = (byte)totalSize;
            sendBuffer[1] = (byte)(totalSize >> 8);
        }
        else
        {
            sendBuffer[0] = (byte)(totalSize >> 8);
            sendBuffer[1] = (byte)totalSize;
        }

        var payload = dataBuffer.ToArraySegment(payloadStart, payloadLen);
        Buffer.BlockCopy(payload.Array, payload.Offset, sendBuffer, Session.HeaderSize, payloadLen);
        return new ArraySegment<byte>(sendBuffer);
    }
}
