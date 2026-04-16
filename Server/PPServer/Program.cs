using System.Net;
using PPServer.Managers;
using PPServer.Network;

var port = args.Length > 0 && int.TryParse(args[0], out var p) ? p : 7000;
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

var rooms = new RoomManager();
var listener = new Listener(new IPEndPoint(IPAddress.Any, port), rooms);
await listener.RunAsync(cts.Token);

return 0;
