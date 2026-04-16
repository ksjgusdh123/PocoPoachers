using System.Collections.Concurrent;
using PPServer.Network;
using PPServer.Protocols;

namespace PPServer.Managers;

public sealed class RoomManager
{
    private readonly ConcurrentDictionary<Guid, ClientSession> _sessions = new();

    public void Register(ClientSession session) => _sessions[session.Id] = session;

    public void Unregister(ClientSession session) => _sessions.TryRemove(session.Id, out _);

    public async Task BroadcastAsync(Packet packet, ClientSession? except, CancellationToken cancellationToken)
    {
        foreach (var kv in _sessions)
        {
            if (except is not null && kv.Value.Id == except.Id)
                continue;

            try
            {
                await kv.Value.SendAsync(packet, cancellationToken).ConfigureAwait(false);
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
