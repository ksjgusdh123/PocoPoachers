using System.Net;
using UnityEngine;

public class NetworkManager : Singleton<NetworkManager>
{
    string host = "127.0.0.1";
    int port = 7000;

    string userName = "Player";
    bool autoLogin = true;

    public Session Session { get; private set; }
    public int MyPlayerId { get; private set; }
    public bool IsLoggedIn { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        if (GetComponent<ObjectManager>() == null)
            gameObject.AddComponent<ObjectManager>();
    }

    void Start()
    {
        Connect();
    }

    public void Connect()
    {
        if (!IPAddress.TryParse(host, out var addr))
        {
            Debug.LogError($"[NetworkManager] Invalid host '{host}'");
            return;
        }

        IPEndPoint endPoint = new IPEndPoint(addr, port);
        Debug.Log($"[NetworkManager] Connecting to {endPoint}");

        Connector connector = new Connector();
        connector.Connect(endPoint, () =>
        {
            Session = new Session(PacketManager.HandlePacket);
            return Session;
        });
    }

    public void NotifySessionConnected()
    {
        Debug.Log("[NetworkManager] Connected");
        if (autoLogin)
            PacketSender.CLoginReq(userName);
    }

    public void NotifySessionDisconnected()
    {
        Debug.Log("[NetworkManager] Disconnected");
        IsLoggedIn = false;
        MyPlayerId = 0;
        ObjectManager.Instance?.Clear();
    }

    public void OnLoginResult(bool success, int playerId, string userName, int level)
    {
        Debug.Log($"[NetworkManager] Login: success={success}, id={playerId}, name='{userName}', level={level}");
        if (success)
        {
            IsLoggedIn = true;
            MyPlayerId = playerId;
        }
    }
}
