namespace Server;

public class Room
{
    public const int MaxGuests = 2;

    public string Code { get; }
    public ClientSession Host { get; }
    public MemberInfoT HostInfo { get; }

    readonly List<(ClientSession Session, MemberInfoT Info)> _guests = new();
    public IReadOnlyList<(ClientSession Session, MemberInfoT Info)> Guests => _guests;

    public bool IsFull => _guests.Count >= MaxGuests;

    public Room(string code, ClientSession host, MemberInfoT hostInfo)
    {
        Code = code;
        Host = host;
        HostInfo = hostInfo;
    }

    public bool TryJoin(ClientSession guest, MemberInfoT guestInfo)
    {
        if (IsFull) return false;
        _guests.Add((guest, guestInfo));
        return true;
    }
}
