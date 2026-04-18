using System.Net.Sockets;
using ServerCore.Managers;
using Packets;

namespace ServerCore.Network;

public sealed class Session
{
    private readonly TcpClient          _client;
    private readonly SessionManager     _sessionManager;
    private readonly SemaphoreSlim      _sendLock = new(1, 1);
    private          NetworkStream?   _stream;
    private          string           _displayName = "guest";

    public Session(TcpClient client, SessionManager sessionManager)
    {
        _client         = client;
        _sessionManager = sessionManager;
        Id              = Guid.NewGuid();
    }

    public Guid Id { get; }

    public string DisplayName => _displayName;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var remote = _client.Client.RemoteEndPoint?.ToString() ?? "?";
        Console.WriteLine($"{remote} {Id}");

        try
        {
            using (_client)
            {
                _stream = _client.GetStream();
                _sessionManager.Add(this);

                while (!cancellationToken.IsCancellationRequested)
                {
                    var body = await PacketHandler.ReadBodyAsync(_stream, cancellationToken).ConfigureAwait(false);
                    if (body is null)
                        break;

                    if (!PacketHandler.TryReadRoot(body, out var root))
                        break;

                    if (!await HandleAsync(root, cancellationToken).ConfigureAwait(false))
                        break;
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (IOException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            _sessionManager.Remove(this);
            Console.WriteLine($"끝 {remote} {_displayName}");
        }
    }

    private async Task<bool> HandleAsync(RootPacket root, CancellationToken cancellationToken)
    {
        switch (root.PayloadType)
        {
            case EPacketType.C_LoginReq:
            {
                var name = root.PayloadAsC_LoginReq().Name;
                if (!string.IsNullOrWhiteSpace(name))
                    _displayName = name.Trim();
                Console.WriteLine($"로그인 {_displayName}");
                return true;
            }

            case EPacketType.C_ChatReq:
            {
                var msg = root.PayloadAsC_ChatReq().Text;
                if (string.IsNullOrEmpty(msg))
                    return true;
                var line = $"[{_displayName}] {msg}";
                var framedPacket = PacketHandler.ChatRes(line);
                await _sessionManager.BroadcastAsync(framedPacket, except: this, cancellationToken).ConfigureAwait(false);
                await SendAsync(framedPacket, cancellationToken).ConfigureAwait(false);
                return true;
            }

            case EPacketType.C_LogoutReq:
            {
                var bye = PacketHandler.ChatRes("bye");
                await SendAsync(bye, cancellationToken).ConfigureAwait(false);
                return false;
            }

            default:
                return true;
        }
    }

    public async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        if (_stream is null)
            return;

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await PacketHandler.SendAsync(_stream, data, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }
}
