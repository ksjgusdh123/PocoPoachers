using System;
using System.Net;
using UnityEngine;

public class NetworkManager : Singleton<NetworkManager>
{
    [SerializeField] string localHost = "127.0.0.1";
    [SerializeField] string remoteHost = "34.47.102.176";
    [SerializeField] int port = 7000;
    [SerializeField] string userName = "Player";
    [SerializeField] bool remoteConnection = false;

    [Tooltip("다른 플레이어 발사 표시용 탄환 프리팹 (보통 로컬 총의 bulletPrefab과 동일)")]
    [SerializeField] GameObject remoteBulletPrefab;

    public Session Session { get; private set; }
    public int MyPlayerId { get; private set; }
    public bool IsLoggedIn { get; private set; }

    public event Action OnConnected;
    public event Action OnDisconnected;

    public string TargetHost => remoteConnection ? remoteHost : localHost;

    public GameObject RemoteBulletPrefab => remoteBulletPrefab;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        if (GetComponent<ObjectManager>() == null)
            gameObject.AddComponent<ObjectManager>();
    }

    void Start()
    {
        Connect(TargetHost, port);
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

    public void NotifySessionConnected()
    {
        Debug.Log("[NetworkManager] Connected");
        PacketSender.CLoginReq(userName);
        OnConnected?.Invoke();
    }

    public void NotifySessionDisconnected()
    {
        Debug.Log("[NetworkManager] Disconnected");
        IsLoggedIn = false;
        MyPlayerId = 0;
        ObjectManager.Instance?.Clear();
        OnDisconnected?.Invoke();
    }

    public void OnLoginResult(bool success, int playerId, string name, int level)
    {
        Debug.Log($"[NetworkManager] Login: success={success}, id={playerId}, name='{name}', level={level}");
        if (success)
        {
            IsLoggedIn = true;
            MyPlayerId = playerId;
        }
    }
}
