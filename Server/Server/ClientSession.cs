using ServerCore;
using System.Net;

namespace Server
{
    public class ClientSession : PacketSession
    {
        public int PlayerId { get; set; }
        public string? UserName { get; set; }
        public Player? Player { get; set; }

        public override void OnConnected(EndPoint endPoint)
        {
            LOG($"OnConnected: {endPoint}");
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            LOG($"OnDisconnected: {endPoint} (PlayerId={PlayerId})");
            SessionManager.Instance.Remove(this);
            if (PlayerId != 0) PlayerManager.Instance.Remove(PlayerId);
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
            PacketManager.HandlePacket(this, buffer);
        }

        public override void OnSend(int numOfBytes)
        {
        }
    }
}
