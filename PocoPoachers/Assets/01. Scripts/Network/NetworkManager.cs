using System;
using System.Collections;
using System.Net;
using System.Runtime.CompilerServices;
using UnityEngine;

public class NetworkManager : Singleton<NetworkManager>
{
    [SerializeField] string localHost = "127.0.0.1";
    [SerializeField] string remoteHost = "34.50.21.90";
    [SerializeField] int port = 7000;
    [SerializeField] bool remoteConnection = false;

    public Session Session { get; private set; }

    [SerializeField] string userName = "Player"; //TODO: UI로 입력받기
    public int MyPlayerId { get; private set; }
    public bool IsLoggedIn { get; private set; }

    public bool IsSinglePlayer { get; private set; }

    public long Rtt { get; private set; }

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action OnSinglePlayerStarted;

    public string TargetHost => remoteConnection ? remoteHost : localHost;


    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        if (GetComponent<ObjectManager>() == null)
            gameObject.AddComponent<ObjectManager>();
    }
    protected override void OnDestroy()
    {
        if (Session != null)
        {
            Session.Disconnect();
        }
    }

    void Start()
    {
        Connect(TargetHost, port);
        StartCoroutine(CoHeartbeatLoop());
    }

    public void Connect(string ip, int p)
    {
        if (string.IsNullOrEmpty(ip) || !IPAddress.TryParse(ip, out var addr))
        {
            Debug.LogError($"[NetworkManager] invalid host");
            return;
        }

        IPEndPoint endPoint = new IPEndPoint(addr, p);
        Debug.Log($"[NetworkManager] Connecting to {endPoint}");

        Connector connector = new Connector();
        connector.Connect(endPoint, () =>
        {
            Session = new Session(PacketManager.HandlePacket);
            return Session;
        });
    }
    public void StartSinglePlayer()
    {
        IsSinglePlayer = true;
        MyPlayerId     = 1;
        IsLoggedIn     = true;
        Session?.Disconnect();
        Session = null;
        OnSinglePlayerStarted?.Invoke();
    }

    public void LeaveGame()
    {
        if (!IsLoggedIn) return;

        if (!IsSinglePlayer)
            PacketBuilder.Send(new C_LogoutT(), C_Logout.Pack, PacketType.C_Logout);

        IsSinglePlayer = false;
        IsLoggedIn     = false;
        MyPlayerId     = 0;
        Session?.Disconnect();
    }

    public void OnSessionConnected()
    {
        Debug.Log("[NetworkManager] Connected");
        PacketBuilder.Send(new C_LoginT { Username = userName ?? string.Empty }, C_Login.Pack, PacketType.C_Login);
        OnConnected?.Invoke();
    }

    public void OnSessionDisconnected()
    {
        Debug.Log("[NetworkManager] Disconnected");
        IsLoggedIn = false;
        MyPlayerId = -1;
        ObjectManager.Instance?.Clear();
        OnDisconnected?.Invoke();
    }

    public void SendHeartbeat()
    {
        if (Session == null || !Session.IsConnected) return;

        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var pkt = new C_HeartbeatT { SendTime = now };
        PacketBuilder.Send(pkt, C_Heartbeat.Pack, PacketType.C_Heartbeat);
    }

    IEnumerator CoHeartbeatLoop()
    {
        while (true)
        {
            if (!IsSinglePlayer && Session != null && Session.IsConnected)
                SendHeartbeat();
            yield return new WaitForSeconds(3.0f);
        }
    }

    public void OnHeartbeatAck(long calculatedRtt)
    {
        Rtt = calculatedRtt;

        if (Rtt > 200) Debug.LogWarning($"[Network] High Ping: {Rtt}ms");
    }

    public void OnLoginResult(bool success, int playerId)
    {
        Debug.Log($"[NetworkManager] Login: success={success}, id={playerId}");
        if (success)
        {
            IsLoggedIn = success;
            MyPlayerId = playerId;
        }
    }
}
