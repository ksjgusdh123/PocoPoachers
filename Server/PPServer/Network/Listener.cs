using System.Net;
using System.Net.Sockets;
using PPServer.Managers;

namespace PPServer.Network;

public sealed class Listener
{
    private readonly IPEndPoint _endPoint;
    private readonly RoomManager _rooms;

    public Listener(IPEndPoint endPoint, RoomManager rooms)
    {
        _endPoint = endPoint;
        _rooms = rooms;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var tcpListener = new TcpListener(_endPoint);
        tcpListener.Start();
        Console.WriteLine($"서버 시작: tcp://{_endPoint.Address}:{_endPoint.Port} (Ctrl+C 종료)");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await tcpListener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                var session = new ClientSession(client, _rooms);
                _ = Task.Run(() => session.RunAsync(cancellationToken), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            tcpListener.Stop();
            Console.WriteLine("서버 종료.");
        }
    }
}
