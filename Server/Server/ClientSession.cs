using ServerCore;
using System.Net;

namespace Server
{
    internal class ClientSession : PacketSession
    {
        public override void OnConnected(EndPoint endPoint)
        {
            Thread.Sleep(5000);
            Disconnect();
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
        }

        public override void OnRecvPacket(ArraySegment<byte> buffer)
        {
        }

        public override void OnSend(int numOfBytes)
        {
        }
    }
}
