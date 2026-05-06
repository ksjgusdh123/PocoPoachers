namespace Server;

public class Room
{
    public string Code { get; }
    public ClientSession Host { get; }
    public PeerInfoT HostInfo { get; }
    public ClientSession? Guest { get; private set; }
    public PeerInfoT? GuestInfo { get; private set; }

    public Room(string code, ClientSession host, PeerInfoT hostInfo)
    {
        Code = code;
        Host = host;
        HostInfo = hostInfo;
    }

    public bool TryJoin(ClientSession guest, PeerInfoT guestInfo)
    {
        if (Guest != null) return false;
        Guest = guest;
        GuestInfo = guestInfo;
        return true;
    }
}
