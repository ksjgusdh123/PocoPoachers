namespace Server;

public class SessionManager
{
    public static SessionManager Instance { get; } = new();

    private readonly object _lock = new();
    private readonly Dictionary<int, ClientSession> _sessions = new();
    private int _nextPlayerId = 0;

    private SessionManager() { }

    public int GenerateId() => Interlocked.Increment(ref _nextPlayerId);

    public void Add(ClientSession session)
    {
        if (session.PlayerId == 0) return;

        int count;
        lock (_lock)
        {
            _sessions[session.PlayerId] = session;
            count = _sessions.Count;
        }
        LOG($"SessionManager.Add: PlayerId={session.PlayerId} (total={count})");
    }

    public void Remove(ClientSession session)
    {
        if (session.PlayerId == 0) return;

        bool removed;
        int count;
        lock (_lock)
        {
            removed = _sessions.Remove(session.PlayerId);
            count = _sessions.Count;
        }
        if (removed)
            LOG($"SessionManager.Remove: PlayerId={session.PlayerId} (total={count})");
    }

    public ClientSession? Find(int playerId)
    {
        lock (_lock)
        {
            return _sessions.TryGetValue(playerId, out var s) ? s : null;
        }
    }

    public int Count
    {
        get { lock (_lock) { return _sessions.Count; } }
    }

    public void StartHeartbeatMonitor(TimeSpan interval, TimeSpan timeout)
    {
        Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(interval);
                var snapshot = Snapshot();
                foreach (var session in snapshot)
                {
                    if ((DateTimeOffset.UtcNow - session.LastPingAt) > timeout)
                    {
                        LOG_W($"[Heartbeat] Timeout PlayerId={session.PlayerId} — disconnecting");
                        session.Disconnect();
                    }
                }
            }
        });
    }

    public List<ClientSession> Snapshot(ClientSession? except = null)
    {
        lock (_lock)
        {
            return except is null
                ? _sessions.Values.ToList()
                : _sessions.Values.Where(s => s != except).ToList();
        }
    }
}
