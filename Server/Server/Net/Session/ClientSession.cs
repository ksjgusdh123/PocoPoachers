using System.Net;
using ServerCore;

namespace Server;

public class ClientSession : PacketSession
{
    public Player? Player { get; set; }
    public int PlayerId => Player?.PlayerId ?? 0;

    public IPEndPoint? RemoteEndPoint { get; private set; }
    public DateTimeOffset LastPingAt { get; set; } = DateTimeOffset.UtcNow;

    public override void OnConnected(EndPoint endPoint)
    {
        RemoteEndPoint = endPoint as IPEndPoint;
        LOG($"OnConnected: {endPoint}");
    }

    public override void OnDisconnected(EndPoint endPoint)
    {
        SessionManager.Instance.Remove(this);
        if (PlayerId != 0)
        {
            //TODO: RoomManager DisConnect처리
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
