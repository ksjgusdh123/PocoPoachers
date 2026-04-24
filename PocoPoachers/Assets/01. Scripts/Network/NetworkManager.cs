using System;
using System.Collections.Generic;
using System.Net;
using Google.FlatBuffers;
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

    readonly Dictionary<PacketType, Action<FlatPacket>> _packetHandlers = new Dictionary<PacketType, Action<FlatPacket>>();

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
        if (GetComponent<ObjectManager>() == null)
            gameObject.AddComponent<ObjectManager>();

        RegisterPacketHandlers();
    }

    void RegisterPacketHandlers()
    {
        _packetHandlers[PacketType.S_LoginRes] = PacketHandlers.S_LoginRes;
        _packetHandlers[PacketType.S_MoveNtf] = PacketHandlers.S_MoveNtf;
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
            Session = new Session(HandleFlatPacket);
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

    void HandleFlatPacket(FlatPacket root)
    {
        if (!_packetHandlers.TryGetValue(root.TypeType, out var handler))
        {
            Debug.LogWarning($"Unknown packet type: {root.TypeType}");
            return;
        }

        handler(root);
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
