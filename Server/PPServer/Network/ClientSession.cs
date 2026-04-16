using System.Buffers.Binary;
using System.Net.Sockets;
using PPServer.Managers;
using PPServer.Protocols;

namespace PPServer.Network;

public sealed class ClientSession
{
    private readonly TcpClient _client;
    private readonly RoomManager _rooms;
    private readonly SemaphoreSlim _sendLock = new(1, 1);
    private NetworkStream? _stream;
    private string _displayName = "guest";

    public ClientSession(TcpClient client, RoomManager rooms)
    {
        _client = client;
        _rooms = rooms;
        Id = Guid.NewGuid();
    }

    public Guid Id { get; }

    public string DisplayName => _displayName;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var remote = _client.Client.RemoteEndPoint?.ToString() ?? "?";
        Console.WriteLine($"연결: {remote} ({Id})");

        try
        {
            using (_client)
            {
                _stream = _client.GetStream();
                _rooms.Register(this);

                var header = new byte[Packet.HeaderSize];
                while (!cancellationToken.IsCancellationRequested)
                {
                    await _stream.ReadExactlyAsync(header.AsMemory(0, Packet.HeaderSize), cancellationToken)
                        .ConfigureAwait(false);

                    var type = (CommandType)header[0];
                    var len = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(1, 2));
                    if (len > Packet.MaxPayloadLength)
                        break;

                    var payload = len == 0 ? Array.Empty<byte>() : new byte[len];
                    if (len > 0)
                    {
                        await _stream.ReadExactlyAsync(payload.AsMemory(0, len), cancellationToken)
                            .ConfigureAwait(false);
                    }

                    var packet = new Packet(type, payload);
                    if (!await HandlePacketAsync(packet, cancellationToken).ConfigureAwait(false))
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
            _rooms.Unregister(this);
            Console.WriteLine($"연결 종료: {remote} ({_displayName})");
        }
    }

    private async Task<bool> HandlePacketAsync(Packet packet, CancellationToken cancellationToken)
    {
        switch (packet.Type)
        {
            case CommandType.Login:
                if (Packet.TryReadPayloadAsUtf8(packet, out var name) && !string.IsNullOrWhiteSpace(name))
                    _displayName = name.Trim();
                Console.WriteLine($"로그인: {_displayName} ({Id})");
                return true;

            case CommandType.Chat:
                if (!Packet.TryReadPayloadAsUtf8(packet, out var msg))
                    return true;
                var line = $"[{_displayName}] {msg}";
                var broadcast = Packet.Text(CommandType.Chat, line);
                await _rooms.BroadcastAsync(broadcast, except: this, cancellationToken).ConfigureAwait(false);
                await SendAsync(broadcast, cancellationToken).ConfigureAwait(false);
                return true;

            case CommandType.Logout:
                await SendAsync(Packet.Text(CommandType.Logout, "bye"), cancellationToken).ConfigureAwait(false);
                return false;

            default:
                return true;
        }
    }

    public async Task SendAsync(Packet packet, CancellationToken cancellationToken)
    {
        if (_stream is null)
            return;

        var total = Packet.GetWireLength(packet);
        var buffer = new byte[total];
        Packet.Write(buffer.AsSpan(), packet);

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _stream.WriteAsync(buffer.AsMemory(0, total), cancellationToken).ConfigureAwait(false);
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }
}
