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
            SessionManager.Instance.StartHeartbeatMonitor(
                interval: TimeSpan.FromSeconds(10),
                timeout:  TimeSpan.FromSeconds(30));
            LOG($"Listening on {endPoint}");

            while (true)
            {
                Thread.Sleep(100);
            }
        }
    }
}
