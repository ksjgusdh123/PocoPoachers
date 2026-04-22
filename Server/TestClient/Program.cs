using Google.FlatBuffers;
using ServerCore;
using System.Net;

namespace TestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Loopback, 7000);

            Connector connector = new Connector();
            connector.Connect(endPoint, () => new ServerSession());

            while (true)
            {
                try
                {
                    Thread.Sleep(100);
                }
                catch (Exception e)
                {
                    LOG_E(e.ToString());
                }
            }
        }
    }
}
