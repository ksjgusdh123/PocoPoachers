using System.Net;
using System.Net.Sockets;
using ServerCore.Managers;

namespace ServerCore.Network;

public sealed class Listener
{
    private readonly IPEndPoint       _endPoint;
    private readonly SessionManager   _sessionManager;

    public Listener(IPEndPoint endPoint, SessionManager sessionManager)
    {
        _endPoint       = endPoint;
        _sessionManager = sessionManager;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        using var tcpListener = new TcpListener(_endPoint);
        tcpListener.Start();
        Console.WriteLine($"tcp://{_endPoint.Address}:{_endPoint.Port}");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var client = await tcpListener.AcceptTcpClientAsync(cancellationToken).ConfigureAwait(false);
                var session = new Session(client, _sessionManager);
                _ = Task.Run(() => session.RunAsync(cancellationToken), cancellationToken);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        finally
        {
            tcpListener.Stop();
            Console.WriteLine("중지");
        }
    }
}
