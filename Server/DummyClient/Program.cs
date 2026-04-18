using System.Net.Sockets;
using Packets;

namespace DummyClient;

internal static class Program
{
    private const string HostAddr = "127.0.0.1";
    private const int Port = 7000;
    private const string DisplayName = "dummy";

    private static async Task Main()
    {
        PacketHandler.ValidateSchema();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        using var client = new TcpClient();
        try
        {
            await client.ConnectAsync(HostAddr, Port, cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            Environment.Exit(1);
        }

        Console.WriteLine($"연결 {HostAddr}:{Port} {DisplayName}");

        await using var stream = client.GetStream();

        await PacketHandler.SendAsync(stream, PacketHandler.Login(DisplayName), cts.Token).ConfigureAwait(false);

        var receive = Task.Run(async () =>
        {
            try
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    var body = await PacketHandler.ReadBodyAsync(stream, cts.Token).ConfigureAwait(false);
                    if (body is null)
                        break;
                    if (!PacketHandler.TryReadRoot(body, out var root))
                        break;
                    var text = PacketHandler.Format(root);
                    if (text is not null)
                        Console.WriteLine($"< {text}");
                    else
                        Console.WriteLine($"< [{root.PayloadType}]");
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
                await PacketHandler.SendAsync(stream, PacketHandler.Chat(line), cts.Token).ConfigureAwait(false);
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
}
