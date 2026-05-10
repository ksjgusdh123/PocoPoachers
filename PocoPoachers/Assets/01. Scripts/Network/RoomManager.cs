using Google.FlatBuffers;
using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class RoomManager : Singleton<RoomManager>
{
    [Header("STUN")]
    [SerializeField] string stunHost = "stun.l.google.com";
    [SerializeField] int    stunPort = 19302;

    public static bool IsHost => Instance != null && Instance._isHost;
    public static bool HasGuests => Instance != null && Instance._hasGuests;
    public static int LastGuestId => Instance != null ? Instance._lastGuestId : 0;

    public bool _hasGuests => _guests.Count > 0;
    private bool _isHost;
    public int  _lastGuestId { get; private set; }

    public event Action          OnRoomJoined;
    public event Action<string>  OnRoomJoinFailed;

    UdpSession _udpSession;
    IPEndPoint _myPublicEp;
    IPEndPoint _hostEp;

    readonly ConcurrentDictionary<int, IPEndPoint>     _guests   = new();
    readonly ConcurrentDictionary<int, UdpHolePuncher> _punchers = new();

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    public void StartAsHost() => CreateOrJoinRoom(true, null);
    public void StartAsGuest(string code) => CreateOrJoinRoom(false, code);

    private void CreateOrJoinRoom(bool isHost, string code)
    {
        _isHost = isHost;
        _guests.Clear();
        _punchers.Clear();

        ThreadPool.QueueUserWorkItem(_ => {
            _udpSession = new UdpSession();
            _udpSession.OnReceived += OnUdpReceived;

            if (!_udpSession.Bind())
            {
                HandleFailure("네트워크 초기화 오류");
                return;
            }
            if (!StunClient.TryGetPublicEndPoint(stunHost, stunPort, _udpSession.Socket, out _myPublicEp))
            {
                HandleFailure("인터넷 연결 오류");
                return;
            }
            var myInfo = GetMySessionInfo();
            MainThreadDispatcher.Enqueue(() => {
                if (_isHost)
                    PacketBuilder.SendToMaster(new C_CreateRoomT { MyInfo = myInfo }, C_CreateRoom.Pack, PacketType.C_CreateRoom);
                else
                    PacketBuilder.SendToMaster(new C_JoinRoomT { SessionCode = code, MyInfo = myInfo }, C_JoinRoom.Pack, PacketType.C_JoinRoom);
            });
        });
    }

    public void ConnectToGuest(MemberInfoT target)
    {
        int id = target.PlayerId;
        IPEndPoint ep = SelectEndPoint(target);

        if (!_isHost)
            _hostEp = ep;

        var puncher = new UdpHolePuncher(_udpSession.Socket);
        _punchers[id] = puncher;

        puncher.OnSuccess += _ => {
            _guests[id] = ep;
            _punchers.TryRemove(id, out _);
            _udpSession.StartReceive();
            MainThreadDispatcher.Enqueue(() => OnRoomJoined?.Invoke());
        };
        puncher.OnFailed += (_, reason) => {
            _punchers.TryRemove(id, out _);
            HandleFailure(reason);
        };
        puncher.Start(ep);
    }

    public void UdpSendToHost<TTable, TObj>(TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> packFunc, PacketType type)
        where TTable : struct where TObj : class
    {
        if (_udpSession == null || _hostEp == null) return;
        _udpSession.Send(PacketBuilder.BuildSegment(data, packFunc, type), _hostEp);
    }

    public void UdpBroadcastToGuests<TTable, TObj>(TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> packFunc, PacketType type, int excludeId = -1)
        where TTable : struct where TObj : class
    {
        if (_guests.IsEmpty || _udpSession == null) return;
        var segment = PacketBuilder.BuildSegment(data, packFunc, type);
        foreach (var kv in _guests)
        {
            if (kv.Key == excludeId) continue;
            _udpSession.Send(segment, kv.Value);
        }
    }

    public void UdpSendToGuest<TTable, TObj>(int playerId, TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> packFunc, PacketType type)
        where TTable : struct where TObj : class
    {
        if (_udpSession == null || !_guests.TryGetValue(playerId, out var ep)) return;
        _udpSession.Send(PacketBuilder.BuildSegment(data, packFunc, type), ep);
    }

    private void OnUdpReceived(ArraySegment<byte> data, IPEndPoint sender)
    {
        int senderId = 0;
        foreach (var kv in _guests)
            if (kv.Value.Equals(sender)) { senderId = kv.Key; break; }

        int captured = senderId;
        MainThreadDispatcher.Enqueue(() =>
        {
            _lastGuestId = captured;
            PacketManager.HandlePacket(data);
            _lastGuestId = 0;
        });
    }

    private IPEndPoint SelectEndPoint(MemberInfoT info)
    {
        if (!string.IsNullOrEmpty(info.PrivateIp) && info.PrivatePort != 0 && IsOnSameLan(info.PrivateIp))
            return new IPEndPoint(IPAddress.Parse(info.PrivateIp), info.PrivatePort);
        return new IPEndPoint(IPAddress.Parse(info.PublicIp), info.PublicPort);
    }

    private bool IsOnSameLan(string otherPrivateIp)
    {
        string myIp = GetLocalPrivateIp();
        var my = myIp.Split('.');
        var other = otherPrivateIp.Split('.');
        if (my.Length < 3 || other.Length < 3) return false;
        return my[0] == other[0] && my[1] == other[1] && my[2] == other[2];
    }

    private string GetLocalPrivateIp()
    {
        using Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0);
        s.Connect("8.8.8.8", 65530);
        return (s.LocalEndPoint as IPEndPoint).Address.ToString();
    }

    public MemberInfoT GetMySessionInfo() => new MemberInfoT
    {
        PlayerId   = NetworkManager.Instance.MyPlayerId,
        PublicIp   = _myPublicEp.Address.ToString(),
        PublicPort = (ushort)_myPublicEp.Port,
        PrivateIp  = GetLocalPrivateIp(),
        PrivatePort = (ushort)_udpSession.LocalEndPoint.Port,
    };

    public void LeaveRoom()
    {
        foreach (var p in _punchers.Values) p.Stop();
        _punchers.Clear();
        _guests.Clear();
        _udpSession?.Close();
        _udpSession = null;
        _hostEp = null;
        _isHost = false;
    }

    public void HandleFailure(string msg)
    {
        MainThreadDispatcher.Enqueue(() => OnRoomJoinFailed?.Invoke(msg));
    }

    protected override void OnDestroy()
    {
        foreach (var p in _punchers.Values) p.Stop();
        _udpSession?.Close();
        base.OnDestroy();
    }
}

