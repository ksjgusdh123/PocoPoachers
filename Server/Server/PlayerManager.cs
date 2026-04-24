namespace Server;

public class PlayerManager
{
    public static PlayerManager Instance { get; } = new();

    private readonly object _lock = new();
    private readonly Dictionary<int, Player> _players = new();

    private PlayerManager() { }

    public Player CreatePlayer(int playerId, string userName)
    {
        var player = new Player { PlayerId = playerId, UserName = userName };
        lock (_lock) { _players[playerId] = player; }
        LOG($"PlayerManager.Create: PlayerId={playerId}, UserName='{userName}'");
        return player;
    }

    public void Remove(int playerId)
    {
        bool removed;
        lock (_lock) { removed = _players.Remove(playerId); }
        if (removed) LOG($"PlayerManager.Remove: PlayerId={playerId}");
    }

    public Player? Find(int playerId)
    {
        lock (_lock) { return _players.TryGetValue(playerId, out var p) ? p : null; }
    }

    public List<Player> Snapshot()
    {
        lock (_lock) { return _players.Values.ToList(); }
    }

    public int Count
    {
        get { lock (_lock) { return _players.Count; } }
    }
}
