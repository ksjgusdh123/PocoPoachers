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
        FlatPacketCodec.EnsureRuntimeMatchesSchema();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var rooms = new RoomManager();
        var listener = new Listener(new IPEndPoint(IPAddress.Any, ListenPort), rooms);
        await listener.RunAsync(cts.Token).ConfigureAwait(false);
    }
}
