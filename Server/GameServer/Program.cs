using System.Net;
using Packets;
using ServerCore.Managers;
using ServerCore.Network;

namespace GameServer;

internal static class Program
{
    private const int ListenPort = 7000;

    private static async Task Main()
    {
        PacketHandler.ValidateSchema();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var sessions = new SessionManager();
        var listener = new Listener(new IPEndPoint(IPAddress.Any, ListenPort), sessions);
        await listener.RunAsync(cts.Token).ConfigureAwait(false);
    }
}
