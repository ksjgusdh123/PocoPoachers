using System.Buffers.Binary;
using System.Net.Sockets;
using Packets;

namespace DummyClient;

internal static class Program
{
    private const string Host = "127.0.0.1";
    private const int Port = 7000;
    private const string DisplayName = "dummy";

    private static async Task Main()
    {
        FlatPacketCodec.EnsureRuntimeMatchesSchema();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(Host, Port, cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.Exit(1);
        }

        Console.WriteLine($"연결 {Host}:{Port} {DisplayName}");

        await using var stream = client.GetStream();

        await SendFramedAsync(stream, FlatPacketCodec.BuildLogin(DisplayName), cts.Token).ConfigureAwait(false);

        var receive = Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var body = await ReadFramedAsync(stream, cts.Token).ConfigureAwait(false);
                    if (body is null)
                        break;
                    if (!FlatPacketCodec.TryParseRoot(body, out var root))
                        break;
                    var text = FlatPacketCodec.DescribeForClientLog(root);
                    if (text is not null)
                        Console.WriteLine($"< {text}");
                    else
                        Console.WriteLine($"< [{root.BodyType}]");
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
                await SendFramedAsync(stream, FlatPacketCodec.BuildChat(line), cts.Token).ConfigureAwait(false);
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
    }

    private static async Task SendFramedAsync(NetworkStream stream, ReadOnlyMemory<byte> framed,
        CancellationToken cancellationToken)
    {
        await stream.WriteAsync(framed, cancellationToken).ConfigureAwait(false);
        await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private static async Task<byte[]?> ReadFramedAsync(NetworkStream stream, CancellationToken cancellationToken)
    {
        var lenBuf = new byte[FlatPacketCodec.FrameHeaderSize];
        await stream.ReadExactlyAsync(lenBuf.AsMemory(0, lenBuf.Length), cancellationToken).ConfigureAwait(false);
        var len = BinaryPrimitives.ReadUInt16LittleEndian(lenBuf);
        if (len is 0 or > FlatPacketCodec.MaxFrameBodyLength)
            return null;

        var body = new byte[len];
        await stream.ReadExactlyAsync(body.AsMemory(0, len), cancellationToken).ConfigureAwait(false);
        return body;
    }
}
