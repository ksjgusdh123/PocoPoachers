using System.Linq;

namespace Server;

public class RoomManager
{
    public static RoomManager Instance { get; } = new();

    RoomManager() { }

}
