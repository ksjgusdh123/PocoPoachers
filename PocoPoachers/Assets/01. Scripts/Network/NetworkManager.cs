using System;
using System.Collections.Generic;
using System.Net;
using Google.FlatBuffers;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }

    string host = "127.0.0.1";
    int port = 7000;

    string userName = "Player";
    bool autoLogin = true;

    public Session Session { get; private set; }
    public int MyPlayerId { get; private set; }
    public bool IsLoggedIn { get; private set; }

    NetObjectManager _netObjects;

    readonly Dictionary<PacketType, Action<FlatPacket>> _packetHandlers = new Dictionary<PacketType, Action<FlatPacket>>();

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        if (GetComponent<NetObjectManager>() == null)
            gameObject.AddComponent<NetObjectManager>();
        _netObjects = GetComponent<NetObjectManager>();

        RegisterPacketHandlers();
    }

    void RegisterPacketHandlers()
    {
        _packetHandlers[PacketType.S_LoginRes] = HandleLoginResult;
        _packetHandlers[PacketType.S_MoveNtf] = HandleMoveUpdate;
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
        {
            if (Session == null) { Debug.LogWarning("[NetworkManager] auto login: no session"); return; }
            Session.Send(MakePacket.CLoginReq(userName));
        }
    }

    public void NotifySessionDisconnected()
    {
        Debug.Log("[NetworkManager] Disconnected");
        IsLoggedIn = false;
        MyPlayerId = 0;
        _netObjects.ClearRemotePlayers();
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

    void HandleLoginResult(FlatPacket root)
    {
        var res = root.TypeAsS_LoginRes();
        var ui = res.UserInfo;
        bool success = res.Success;
        int playerId = ui?.Id ?? 0;
        string userName = ui?.Name ?? string.Empty;
        int level = ui?.Level ?? 0;

        MainThreadDispatcher.Enqueue(() =>
        {
            Debug.Log($"[NetworkManager] Login: success={success}, id={playerId}, name='{userName}', level={level}");
            if (success)
            {
                IsLoggedIn = true;
                MyPlayerId = playerId;
            }
        });
    }

    void HandleMoveUpdate(FlatPacket root)
    {
        var ntf = root.TypeAsS_MoveNtf();
        float x = ntf.Pos?.X ?? 0f;
        float y = ntf.Pos?.Y ?? 0f;
        float z = ntf.Pos?.Z ?? 0f;
        int playerId = ntf.PlayerId;
        Vector3 pos = new Vector3(x, y, z);
        float rotation = ntf.Rotation;
        sbyte moveType = ntf.MoveType;

        MainThreadDispatcher.Enqueue(() => _netObjects.ApplyRemotePlayerMove(playerId, pos, rotation, moveType));
    }

    void OnApplicationQuit()
    {
        try { Session?.Disconnect(); } catch { }
    }
}
