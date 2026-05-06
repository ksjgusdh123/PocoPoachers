namespace Server;

public class P2PRoom
{
    public string SessionCode { get; }
    public ClientSession Host { get; }
    public string HostPublicIp { get; }
    public ushort HostPublicPort { get; }
    public string HostPrivateIp { get; }
    public ushort HostPrivatePort { get; }

    public P2PRoom(string sessionCode, ClientSession host,
        string hostPublicIp, ushort hostPublicPort,
        string hostPrivateIp, ushort hostPrivatePort)
    {
        SessionCode    = sessionCode;
        Host           = host;
        HostPublicIp   = hostPublicIp;
        HostPublicPort = hostPublicPort;
        HostPrivateIp  = hostPrivateIp;
        HostPrivatePort = hostPrivatePort;
    }
}

public class RoomManager
{
    public static RoomManager Instance { get; } = new();

    private readonly object _lock = new();
    private readonly Dictionary<string, P2PRoom> _rooms = new();
    private readonly Dictionary<int, string> _hostToCode = new();

    private RoomManager() { }

    public bool TryRegister(string sessionCode, ClientSession host,
        string publicIp, ushort publicPort, string privateIp, ushort privatePort)
    {
        lock (_lock)
        {
            if (_rooms.ContainsKey(sessionCode)) return false;

            var room = new P2PRoom(sessionCode, host, publicIp, publicPort, privateIp, privatePort);
            _rooms[sessionCode] = room;
            _hostToCode[host.PlayerId] = sessionCode;
        }
        LOG($"[RoomManager] Register session={sessionCode} host={host.PlayerId}");
        return true;
    }

    public P2PRoom? TryJoin(string sessionCode, ClientSession peer)
    {
        P2PRoom? room;
        lock (_lock)
        {
            _rooms.TryGetValue(sessionCode, out room);
        }
        if (room == null)
        {
            LOG_W($"[RoomManager] Join failed — session not found: {sessionCode}");
            return null;
        }
        LOG($"[RoomManager] Peer joined session={sessionCode} peer={peer.PlayerId}");
        return room;
    }

    public void RemoveByHost(int hostPlayerId)
    {
        lock (_lock)
        {
            if (!_hostToCode.TryGetValue(hostPlayerId, out var code)) return;
            _rooms.Remove(code);
            _hostToCode.Remove(hostPlayerId);
            LOG($"[RoomManager] Removed session={code} (host disconnected)");
        }
    }
}
