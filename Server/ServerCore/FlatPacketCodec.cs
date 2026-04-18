using System.Buffers.Binary;
using Google.FlatBuffers;

namespace Packets;

public static class FlatPacketCodec
{
    public const int FrameHeaderSize = 2;
    public const int MaxFrameBodyLength = ushort.MaxValue;

    public static void EnsureRuntimeMatchesSchema() => RootPacket.ValidateVersion();

    public static byte[] BuildLogin(string name)
    {
        var trimmed = name.Trim();
        var fbb = new FlatBufferBuilder(Math.Max(64, trimmed.Length * 4 + 64));
        var nameOff = fbb.CreateString(trimmed);
        var loginOff = LoginBody.CreateLoginBody(fbb, nameOff);
        var rootOff = RootPacket.CreateRootPacket(fbb, PacketBody.LoginBody, loginOff.Value);
        RootPacket.FinishRootPacketBuffer(fbb, rootOff);
        return Framed(fbb);
    }

    public static byte[] BuildChat(string text)
    {
        var fbb = new FlatBufferBuilder(Math.Max(64, text.Length * 4 + 64));
        var textOff = fbb.CreateString(text);
        var chatOff = ChatBody.CreateChatBody(fbb, textOff);
        var rootOff = RootPacket.CreateRootPacket(fbb, PacketBody.ChatBody, chatOff.Value);
        RootPacket.FinishRootPacketBuffer(fbb, rootOff);
        return Framed(fbb);
    }

    private static byte[] Framed(FlatBufferBuilder fbb)
    {
        var body = fbb.SizedByteArray();
        if (body.Length > MaxFrameBodyLength)
            throw new InvalidOperationException("payload too large");

        var frame = new byte[FrameHeaderSize + body.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(frame.AsSpan(0, FrameHeaderSize), (ushort)body.Length);
        body.AsSpan().CopyTo(frame.AsSpan(FrameHeaderSize));
        return frame;
    }

    public static bool TryParseRoot(byte[] body, out RootPacket root)
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

    public static string? DescribeForClientLog(RootPacket root) => root.BodyType switch
    {
        PacketBody.LoginBody => root.BodyAsLoginBody().Name,
        PacketBody.ChatBody => root.BodyAsChatBody().Text,
        PacketBody.LogoutBody => "logout",
        _ => null
    };
}
