using System.Buffers.Binary;
using System.Net.Sockets;
using ServerCore.Managers;
using Packets;

namespace ServerCore.Network;

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
        Console.WriteLine($"{remote} {Id}");

        try
        {
            using (_client)
            {
                _stream = _client.GetStream();
                _rooms.Register(this);

                var lenBuf = new byte[FlatPacketCodec.FrameHeaderSize];
                while (!cancellationToken.IsCancellationRequested)
                {
                    await _stream.ReadExactlyAsync(lenBuf.AsMemory(0, lenBuf.Length), cancellationToken)
                        .ConfigureAwait(false);

                    var len = BinaryPrimitives.ReadUInt16LittleEndian(lenBuf);
                    if (len is 0 or > FlatPacketCodec.MaxFrameBodyLength)
                        break;

                    var body = new byte[len];
                    await _stream.ReadExactlyAsync(body.AsMemory(0, len), cancellationToken).ConfigureAwait(false);

                    if (!FlatPacketCodec.TryParseRoot(body, out var root))
                        break;

                    if (!await HandleRootAsync(root, cancellationToken).ConfigureAwait(false))
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
            Console.WriteLine($"끝 {remote} {_displayName}");
        }
    }

    private async Task<bool> HandleRootAsync(RootPacket root, CancellationToken cancellationToken)
    {
        switch (root.BodyType)
        {
            case PacketBody.LoginBody:
            {
                var name = root.BodyAsLoginBody().Name;
                if (!string.IsNullOrWhiteSpace(name))
                    _displayName = name.Trim();
                Console.WriteLine($"로그인 {_displayName}");
                return true;
            }

            case PacketBody.ChatBody:
            {
                var msg = root.BodyAsChatBody().Text;
                if (string.IsNullOrEmpty(msg))
                    return true;
                var line = $"[{_displayName}] {msg}";
                var wire = FlatPacketCodec.BuildChat(line);
                await _rooms.BroadcastAsync(wire, except: this, cancellationToken).ConfigureAwait(false);
                await SendAsync(wire, cancellationToken).ConfigureAwait(false);
                return true;
            }

            case PacketBody.LogoutBody:
            {
                var bye = FlatPacketCodec.BuildChat("bye");
                await SendAsync(bye, cancellationToken).ConfigureAwait(false);
                return false;
            }

            default:
                return true;
        }
    }

    public async Task SendAsync(ReadOnlyMemory<byte> framedWire, CancellationToken cancellationToken)
    {
        if (_stream is null)
            return;

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await _stream.WriteAsync(framedWire, cancellationToken).ConfigureAwait(false);
            await _stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }
}
