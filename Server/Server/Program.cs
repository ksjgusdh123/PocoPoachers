using System.Net;
using ServerCore;

namespace Server
{
    class Program
    {
        static Listener _listener = new Listener();

        static void Main(string[] args)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 7000);

            _listener.Init(endPoint, () => { return new ClientSession(); });
            LOG($"Listening on {endPoint}");

            while (true)
            {
                Thread.Sleep(100);
            }
        }
    }
}
