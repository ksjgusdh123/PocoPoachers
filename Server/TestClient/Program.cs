using ServerCore;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TestClient
{
    class GameSession : Session
    {
        public override void OnConnected(EndPoint endPoint)
        {
            LOG();

            byte[] sendBuff = Encoding.UTF8.GetBytes("hello");
            Send(new ArraySegment<byte>(sendBuff));
        }

        public override void OnDisconnected(EndPoint endPoint)
        {
            LOG();
        }

        public override int OnRecv(ArraySegment<byte> buffer)
        {
            string data = Encoding.UTF8.GetString(buffer.Array!, buffer.Offset, buffer.Count);
            LOG($"{data}");
            return buffer.Count;
        }

        public override void OnSend(int numOfBytes)
        {
            LOG();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            string host = Dns.GetHostName();
            IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = ipHost.AddressList[0];
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 7000);

            Connector connector = new Connector();
            connector.Connect(endPoint, () => { return new GameSession(); });
            while (true)
            {
                try
                {

                }
                catch (Exception e)
                {
                    LOG_E(e.ToString());
                }
            }

        }
    }
}