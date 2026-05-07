namespace Server;

public class Room
{
    public const int MaxGuests = 2;

    public string Code { get; }
    public ClientSession Host { get; }
    public PeerInfoT HostInfo { get; }

    readonly List<(ClientSession Session, PeerInfoT Info)> _guests = new();
    public IReadOnlyList<(ClientSession Session, PeerInfoT Info)> Guests => _guests;

    public bool IsFull => _guests.Count >= MaxGuests;

    public Room(string code, ClientSession host, PeerInfoT hostInfo)
    {
        Code = code;
        Host = host;
        HostInfo = hostInfo;
    }

    public bool TryJoin(ClientSession guest, PeerInfoT guestInfo)
    {
        if (IsFull) return false;
        _guests.Add((guest, guestInfo));
        return true;
    }
}
