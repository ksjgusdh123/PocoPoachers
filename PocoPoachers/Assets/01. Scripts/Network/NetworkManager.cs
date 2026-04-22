using System;
using System.Net;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    [Header("Server")]
    [SerializeField] string host = "127.0.0.1";
    [SerializeField] int port = 7000;

    [Header("Login")]
    [SerializeField] string userName = "Player";
    [SerializeField] bool autoLogin = true;

    public ServerSession Session { get; private set; }
    public int MyPlayerId { get; private set; }
    public bool IsLoggedIn { get; private set; }

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<LoginResultData> OnLogin;
    public event Action<MoveUpdateData> OnOtherPlayerMove;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
            Session = new ServerSession();
            Session.OnConnectedEvent += HandleConnected;
            Session.OnDisconnectedEvent += HandleDisconnected;
            Session.OnLoginResult += HandleLoginResult;
            Session.OnMoveUpdate += HandleMoveUpdate;
            return Session;
        });
    }

    void HandleConnected()
    {
        Debug.Log("[NetworkManager] Connected");
        OnConnected?.Invoke();
        if (autoLogin) SendLogin(userName);
    }

    void HandleDisconnected()
    {
        Debug.Log("[NetworkManager] Disconnected");
        IsLoggedIn = false;
        MyPlayerId = 0;
        OnDisconnected?.Invoke();
    }

    void HandleLoginResult(LoginResultData data)
    {
        Debug.Log($"[NetworkManager] Login: success={data.Success}, id={data.PlayerId}, name='{data.UserName}', level={data.Level}");
        if (data.Success)
        {
            IsLoggedIn = true;
            MyPlayerId = data.PlayerId;
        }
        OnLogin?.Invoke(data);
    }

    void HandleMoveUpdate(MoveUpdateData data)
    {
        OnOtherPlayerMove?.Invoke(data);
    }

    public void SendLogin(string name)
    {
        if (Session == null) { Debug.LogWarning("[NetworkManager] SendLogin: no session"); return; }
        Session.SendLoginReq(name);
    }

    public void SendMove(Vector3 pos, float rotation, sbyte moveType = 0)
    {
        if (Session == null || !IsLoggedIn) return;
        Session.SendMoveReq(pos, rotation, moveType);
    }

    void OnApplicationQuit()
    {
        try { Session?.Disconnect(); } catch { }
    }
}
