using System;
using System.Collections;
using System.Net;
using UnityEngine;

public class NetworkManager : Singleton<NetworkManager>
{
    [SerializeField] string localHost = "127.0.0.1";
    [SerializeField] string remoteHost = "34.50.21.90";
    [SerializeField] int port = 7000;
    [SerializeField] string userName = "Player";
    [SerializeField] bool remoteConnection = false;

    [Tooltip("다른 플레이어 발사 표시용 탄환 프리팹 (보통 로컬 총의 bulletPrefab과 동일)")]
    [SerializeField] GameObject remoteBulletPrefab;

    public Session Session { get; private set; }
    public int MyPlayerId { get; private set; }
    public bool IsLoggedIn { get; private set; }
    public long Rtt { get; private set; }

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
        StartCoroutine(CoPingLoop());
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
        PacketBuilder.Send(new C_LoginT { Username = userName ?? string.Empty }, C_Login.Pack, PacketType.C_Login);
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

    public void SendPing()
    {
        //TODO: Send Pkt

    }

    IEnumerator CoPingLoop()
    {
        while (true)
        {
            if (Session != null && Session.IsConnected)
            {
                SendPing();
            }
            yield return new WaitForSeconds(2.0f);
        }
    }

    public void OnPongRes(long rtt)
    {
        Rtt = rtt;
        Debug.Log($"[NetworkManager] RTT: {rtt}ms");
    }

    public void OnLoginResult(bool success, int playerId)
    {
        Debug.Log($"[NetworkManager] Login: success={success}, id={playerId}");
        if (success)
        {
            IsLoggedIn = true;
            MyPlayerId = playerId;
        }
    }
}
