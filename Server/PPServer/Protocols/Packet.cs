using System.Buffers.Binary;

namespace PPServer.Protocols;

public readonly record struct Packet(CommandType Type, ReadOnlyMemory<byte> Payload)
{
    public const int HeaderSize = 3;
    public const int MaxPayloadLength = ushort.MaxValue;

    public static Packet Text(CommandType type, string text)
    {
        var bytes = string.IsNullOrEmpty(text) ? [] : System.Text.Encoding.UTF8.GetBytes(text);
        if (bytes.Length > MaxPayloadLength)
            throw new ArgumentOutOfRangeException(nameof(text), "페이로드가 너무 큽니다.");
        return new Packet(type, bytes);
    }

    public static int GetWireLength(Packet packet) => HeaderSize + packet.Payload.Length;

    public static void Write(Span<byte> destination, Packet packet)
    {
        if (packet.Payload.Length > MaxPayloadLength)
            throw new ArgumentOutOfRangeException(nameof(packet));

        destination[0] = (byte)packet.Type;
        BinaryPrimitives.WriteUInt16LittleEndian(destination[1..], (ushort)packet.Payload.Length);
        packet.Payload.Span.CopyTo(destination[HeaderSize..]);
    }

    public static bool TryReadPayloadAsUtf8(Packet packet, out string text)
    {
        try
        {
            text = System.Text.Encoding.UTF8.GetString(packet.Payload.Span);
            return true;
        }
        catch
        {
            text = string.Empty;
            return false;
        }
    }
}
