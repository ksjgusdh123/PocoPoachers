using System.Buffers;
using System.Net;
using System.Net.Sockets;

// 기본 7000; 인자로 포트 지정 가능: dotnet run -- 9090
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

var listener = new TcpListener(IPAddress.Any, port);
listener.Start();
Console.WriteLine($"에코 서버 시작: tcp://0.0.0.0:{port} (Ctrl+C 종료)");

try
{
    while (!cts.Token.IsCancellationRequested)
    {
        var client = await listener.AcceptTcpClientAsync(cts.Token);
        _ = Task.Run(() => HandleClientAsync(client, cts.Token), cts.Token);
    }
}
catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
{
    // 정상 종료
}
finally
{
    listener.Stop();
    Console.WriteLine("서버 종료.");
}

return 0;

static async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
{
    var remote = client.Client.RemoteEndPoint?.ToString() ?? "?";
    Console.WriteLine($"연결: {remote}");

    using (client)
    {
        try
        {
            await using var stream = client.GetStream();
            var buffer = ArrayPool<byte>.Shared.Rent(8192);
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)
                        .ConfigureAwait(false);
                    if (read == 0)
                        break;

                    await stream.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
                    await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // 종료 시 무시
        }
        catch (IOException)
        {
            // 클라이언트가 끊은 경우 등
        }
        catch (ObjectDisposedException)
        {
            // 소켓이 이미 닫힌 경우
        }
    }

    Console.WriteLine($"연결 종료: {remote}");
}
