using System.Linq;

namespace Server;

public class RoomManager
{
    public static RoomManager Instance { get; } = new();

    private readonly object _lock = new();
    private readonly Dictionary<string, Room> _rooms = new();

    RoomManager() { }

    public Room CreateRoom(ClientSession host, PeerInfoT hostInfo)
    {
        lock (_lock)
        {
            string code;
            do { code = GenerateCode(); } while (_rooms.ContainsKey(code));
            var room = new Room(code, host, hostInfo);
            _rooms[code] = room;
            LOG($"RoomManager.Create: code={code} host={host.PlayerId}");
            return room;
        }
    }

    public Room? FindRoom(string code)
    {
        lock (_lock)
        {
            return _rooms.TryGetValue(code, out var room) ? room : null;
        }
    }

    public void RemoveRoom(string code)
    {
        lock (_lock)
        {
            if (_rooms.Remove(code))
                LOG($"RoomManager.Remove: code={code}");
        }
    }

    public void RemoveBySession(ClientSession session)
    {
        lock (_lock)
        {
            var codes = _rooms.Values
                .Where(r => r.Host == session || r.Guest == session)
                .Select(r => r.Code)
                .ToList();
            foreach (var code in codes)
            {
                _rooms.Remove(code);
                LOG($"RoomManager.Remove: code={code} (session disconnected)");
            }
        }
    }

    private static string GenerateCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Range(0, 6).Select(_ => chars[Random.Shared.Next(chars.Length)]).ToArray());
    }
}
