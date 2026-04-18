using System.Collections.Concurrent;
using ServerCore.Network;

namespace ServerCore.Managers;

public sealed class SessionManager
{
    private readonly ConcurrentDictionary<Guid, Session> _sessions = new();

    public void Add(Session session) => _sessions[session.Id] = session;

    public void Remove(Session session) => _sessions.TryRemove(session.Id, out _);

    public async Task BroadcastAsync(ReadOnlyMemory<byte> data, Session? except, CancellationToken cancellationToken)
    {
        foreach (var kv in _sessions)
        {
            if (except is not null && kv.Value.Id == except.Id)
                continue;

            try
            {
                await kv.Value.SendAsync(data, cancellationToken).ConfigureAwait(false);
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
