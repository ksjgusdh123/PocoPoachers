using System.Buffers.Binary;
using Google.FlatBuffers;

namespace Packets;

public static class PacketHandler
{
    public const int HeaderSize = 2;
    public const int MaxBody = ushort.MaxValue;

    public static void ValidateSchema() => RootPacket.ValidateVersion();

    public static async Task<byte[]?> ReadBodyAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        var lenBuf = new byte[HeaderSize];
        await stream.ReadExactlyAsync(lenBuf.AsMemory(0, lenBuf.Length), cancellationToken).ConfigureAwait(false);
        var len = BinaryPrimitives.ReadUInt16LittleEndian(lenBuf);
        if (len is 0 or > MaxBody)
            return null;
        var body = new byte[len];
        await stream.ReadExactlyAsync(body.AsMemory(0, len), cancellationToken).ConfigureAwait(false);
        return body;
    }

    public static async Task SendAsync(Stream stream, ReadOnlyMemory<byte> framed,
        CancellationToken cancellationToken = default)
    {
        await stream.WriteAsync(framed, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    public static byte[] Login(string name)
    {
        var trimmed = name.Trim();
        var fbb = new FlatBufferBuilder(Math.Max(64, trimmed.Length * 4 + 64));
        var nameOff = fbb.CreateString(trimmed);
        var loginOff = C_LoginReq.CreateC_LoginReq(fbb, nameOff);
        var rootOff = RootPacket.CreateRootPacket(fbb, EPacketType.C_LoginReq, loginOff.Value);
        RootPacket.FinishRootPacketBuffer(fbb, rootOff);
        return Wrap(fbb);
    }

    public static byte[] Chat(string text)
    {
        var fbb = new FlatBufferBuilder(Math.Max(64, text.Length * 4 + 64));
        var textOff = fbb.CreateString(text);
        var chatOff = C_ChatReq.CreateC_ChatReq(fbb, textOff);
        var rootOff = RootPacket.CreateRootPacket(fbb, EPacketType.C_ChatReq, chatOff.Value);
        RootPacket.FinishRootPacketBuffer(fbb, rootOff);
        return Wrap(fbb);
    }

    public static byte[] ChatRes(string text)
    {
        var fbb = new FlatBufferBuilder(Math.Max(64, text.Length * 4 + 64));
        var textOff = fbb.CreateString(text);
        var resOff = S_ChatRes.CreateS_ChatRes(fbb, textOff);
        var rootOff = RootPacket.CreateRootPacket(fbb, EPacketType.S_ChatRes, resOff.Value);
        RootPacket.FinishRootPacketBuffer(fbb, rootOff);
        return Wrap(fbb);
    }

    private static byte[] Wrap(FlatBufferBuilder fbb)
    {
        var body = fbb.SizedByteArray();
        if (body.Length > MaxBody)
            throw new InvalidOperationException("payload too large");

        var frame = new byte[HeaderSize + body.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(0, HeaderSize), (ushort)body.Length);
        body.AsSpan().CopyTo(frame.AsSpan(HeaderSize));
        return frame;
    }

    public static bool TryReadRoot(byte[] body, out RootPacket root)
    {
        try
        {
            var bb = new ByteBuffer(body);
            root = RootPacket.GetRootAsRootPacket(bb);
            return true;
        }
        catch
        {
            root = default;
            return false;
        }
    }

    public static string? Format(RootPacket root) => root.PayloadType switch
    {
        EPacketType.C_LoginReq => root.PayloadAsC_LoginReq().Name,
        EPacketType.C_ChatReq => root.PayloadAsC_ChatReq().Text,
        EPacketType.C_LogoutReq => "logout",
        EPacketType.S_ChatRes => root.PayloadAsS_ChatRes().Text,
        _ => null
    };
}
