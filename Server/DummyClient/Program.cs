using System.Buffers.Binary;
using System.Net.Sockets;
using PPServer.Protocols;

static async Task SendPacketAsync(NetworkStream stream, Packet packet, CancellationToken cancellationToken)
{
    var total = Packet.GetWireLength(packet);
    var buffer = new byte[total];
    Packet.Write(buffer.AsSpan(), packet);
    await stream.WriteAsync(buffer.AsMemory(0, total), cancellationToken).ConfigureAwait(false);
    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
}

static async Task<Packet?> ReadPacketAsync(NetworkStream stream, CancellationToken cancellationToken)
{
    var header = new byte[Packet.HeaderSize];
    await stream.ReadExactlyAsync(header.AsMemory(0, Packet.HeaderSize), cancellationToken).ConfigureAwait(false);
    var type = (CommandType)header[0];
    var len = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(1, 2));
    if (len > Packet.MaxPayloadLength)
        return null;

    var payload = len == 0 ? Array.Empty<byte>() : new byte[len];
    if (len > 0)
        await stream.ReadExactlyAsync(payload.AsMemory(0, len), cancellationToken).ConfigureAwait(false);

    return new Packet(type, payload);
}

var host = args.Length > 0 ? args[0] : "127.0.0.1";
var port = args.Length > 1 && int.TryParse(args[1], out var p) ? p : 7000;
var name = args.Length > 2 ? args[2] : "dummy";

if (port is < 1 or > 65535)
{
    Console.Error.WriteLine("포트는 1~65535 사이여야 합니다.");
    return 1;
}

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

using var client = new TcpClient();
try
{
    await client.ConnectAsync(host, port, cts.Token).ConfigureAwait(false);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"연결 실패: {ex.Message}");
    return 1;
}

Console.WriteLine($"연결됨: {host}:{port} (이름: {name}, 종료: 빈 줄 입력)");

await using var stream = client.GetStream();

await SendPacketAsync(stream, Packet.Text(CommandType.Login, name), cts.Token).ConfigureAwait(false);

var receive = Task.Run(async () =>
{
    try
    {
        while (!cts.Token.IsCancellationRequested)
        {
            var packet = await ReadPacketAsync(stream, cts.Token).ConfigureAwait(false);
            if (packet is null)
                break;
            if (Packet.TryReadPayloadAsUtf8(packet.Value, out var text))
                Console.WriteLine($"< {text}");
            else
                Console.WriteLine($"< [{packet.Value.Type}]");
        }
    }
    catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
    {
    }
    catch (IOException)
    {
    }
});

while (!cts.Token.IsCancellationRequested)
{
    var line = Console.ReadLine();
    if (line is null)
        break;
    if (string.IsNullOrEmpty(line))
    {
        cts.Cancel();
        break;
    }

    try
    {
        await SendPacketAsync(stream, Packet.Text(CommandType.Chat, line), cts.Token).ConfigureAwait(false);
    }
    catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
    {
        break;
    }
}

try
{
    await receive.ConfigureAwait(false);
}
catch
{
}

return 0;
