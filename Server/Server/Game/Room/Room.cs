namespace Server;

public class Room
{
    public const int MaxGuests = 2;

    public string Code { get; }
    public ClientSession Host { get; }
    public NetInfoT HostInfo { get; }

    readonly List<(ClientSession Session, NetInfoT Info)> _guests = new();
    public IReadOnlyList<(ClientSession Session, NetInfoT Info)> Guests => _guests;

    public bool IsFull => _guests.Count >= MaxGuests;

    public Room(string code, ClientSession host, NetInfoT hostInfo)
    {
        Code = code;
        Host = host;
        HostInfo = hostInfo;
    }

    public bool TryJoin(ClientSession guest, NetInfoT guestInfo)
    {
        if (IsFull) return false;
        _guests.Add((guest, guestInfo));
        return true;
    }
}
