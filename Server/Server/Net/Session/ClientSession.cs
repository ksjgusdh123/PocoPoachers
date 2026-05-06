using System.Net;
using ServerCore;

namespace Server;

public class ClientSession : PacketSession
{
    public Player? Player { get; set; }
    public IPEndPoint? RemoteEndPoint { get; private set; }
    public DateTimeOffset LastPingAt { get; set; } = DateTimeOffset.UtcNow;

    public int PlayerId => Player?.PlayerId ?? 0;
    public string UserName => Player?.UserName ?? string.Empty;

    public override void OnConnected(EndPoint endPoint)
    {
        RemoteEndPoint = endPoint as IPEndPoint;
        LOG($"OnConnected: {endPoint}");
    }

    public override void OnDisconnected(EndPoint endPoint)
    {
        LOG($"OnDisconnected: {endPoint} (PlayerId={PlayerId})");
        SessionManager.Instance.Remove(this);
        if (PlayerId != 0)
        {
            PlayerManager.Instance.Remove(PlayerId);
            RoomManager.Instance.RemoveByHost(PlayerId);
        }
    }

    public override void OnRecvPacket(ArraySegment<byte> buffer)
    {
        PacketManager.HandlePacket(this, buffer);
    }

    public override void OnSend(int numOfBytes)
    {
    }
}
