using System.Collections.Concurrent;
using ServerCore.Network;

namespace ServerCore.Managers;

public sealed class RoomManager
{
    private readonly ConcurrentDictionary<Guid, ClientSession> _sessions = new();

    public void Register(ClientSession session) => _sessions[session.Id] = session;

    public void Unregister(ClientSession session) => _sessions.TryRemove(session.Id, out _);

    public async Task BroadcastAsync(ReadOnlyMemory<byte> framedWire, ClientSession? except,
        CancellationToken cancellationToken)
    {
        foreach (var kv in _sessions)
        {
            if (except is not null && kv.Value.Id == except.Id)
                continue;

            try
            {
                await kv.Value.SendAsync(framedWire, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch
            {
            }
        }
    }
}
