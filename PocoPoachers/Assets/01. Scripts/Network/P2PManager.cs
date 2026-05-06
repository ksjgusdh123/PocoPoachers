using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Google.FlatBuffers;
using UnityEngine;

public class P2PManager : Singleton<P2PManager>
{
    [Header("STUN")]
    [SerializeField] string stunHost = "stun.l.google.com";
    [SerializeField] int    stunPort = 19302;

    public bool IsHost             { get; private set; }
    public bool IsConnected        => _peers.Count > 0;
    public int  PeerCount          => _peers.Count;
    public int  LastSenderPlayerId { get; private set; }

    public event Action          OnP2PConnected;
    public event Action<string>  OnP2PFailed;

    Socket     _udpSocket;
    IPEndPoint _myPublicEp;
    Thread     _recvThread;

    readonly ConcurrentDictionary<int, IPEndPoint>     _peers    = new();
    readonly ConcurrentDictionary<int, UdpHolePuncher> _punchers = new();

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    public void StartAsHost() => StartP2P(true, null);
    public void StartAsGuest(string code) => StartP2P(false, code);

    private void StartP2P(bool isHost, string code)
    {
        IsHost = isHost;
        _peers.Clear();
        _punchers.Clear();

        ThreadPool.QueueUserWorkItem(_ => {
            if (!PrepareSocket()) return;
            if (!StunClient.TryGetPublicEndPoint(stunHost, stunPort, _udpSocket, out _myPublicEp))
            {
                HandleFailure("STUN 실패");
                return;
            }
            var myInfo = GetMyPeerInfo();
            MainThreadDispatcher.Enqueue(() => {
                if (IsHost)
                    PacketBuilder.Send(new C_CreateRoomT { MyInfo = myInfo }, C_CreateRoom.Pack, PacketType.C_CreateRoom);
                else
                    PacketBuilder.Send(new C_JoinRoomT { SessionCode = code, MyInfo = myInfo }, C_JoinRoom.Pack, PacketType.C_JoinRoom);
            });
        });
    }

    public void BeginPunch(PeerInfoT target)
    {
        int peerId = target.PlayerId;
        IPEndPoint ep = SelectEndPoint(target);

        var puncher = new UdpHolePuncher(_udpSocket);
        _punchers[peerId] = puncher;

        puncher.OnSuccess += _ => {
            _peers[peerId] = ep;
            _punchers.TryRemove(peerId, out _);

            if (_recvThread == null || !_recvThread.IsAlive)
            {
                _recvThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "P2P-Recv" };
                _recvThread.Start();
            }
            MainThreadDispatcher.Enqueue(() => OnP2PConnected?.Invoke());
        };
        puncher.OnFailed += (_, reason) => {
            _punchers.TryRemove(peerId, out _);
            HandleFailure(reason);
        };
        puncher.Start(ep);
    }

    // 모든 피어에게 전송
    public void SendToAll<TTable, TObj>(TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> packFunc, PacketType type)
        where TTable : struct where TObj : class
    {
        if (_peers.IsEmpty || _udpSocket == null) return;
        try
        {
            var segment = PacketBuilder.BuildSegment(data, packFunc, type);
            foreach (var ep in _peers.Values)
                _udpSocket.SendTo(segment.Array, segment.Offset, segment.Count, SocketFlags.None, ep);
        }
        catch (Exception e) { Debug.LogWarning($"[P2P] SendToAll failed: {e.Message}"); }
    }

    // 특정 피어에게만 raw 바이트 전송
    public void SendTo(int playerId, ArraySegment<byte> segment)
    {
        if (_udpSocket == null || !_peers.TryGetValue(playerId, out var ep)) return;
        try { _udpSocket.SendTo(segment.Array, segment.Offset, segment.Count, SocketFlags.None, ep); }
        catch (Exception e) { Debug.LogWarning($"[P2P] SendTo {playerId} failed: {e.Message}"); }
    }

    public void SendTo<TTable, TObj>(int playerId, TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> packFunc, PacketType type)
        where TTable : struct where TObj : class
    {
        SendTo(playerId, PacketBuilder.BuildSegment(data, packFunc, type));
    }

    // 특정 피어 제외하고 raw 바이트 릴레이 (호스트가 게스트 패킷 중계 시 사용)
    public void RelayExcept(int excludePlayerId, ArraySegment<byte> segment)
    {
        if (_udpSocket == null) return;
        foreach (var kv in _peers)
        {
            if (kv.Key == excludePlayerId) continue;
            try { _udpSocket.SendTo(segment.Array, segment.Offset, segment.Count, SocketFlags.None, kv.Value); }
            catch (Exception e) { Debug.LogWarning($"[P2P] Relay to {kv.Key} failed: {e.Message}"); }
        }
    }

    private void ReceiveLoop()
    {
        byte[] buffer = new byte[2048];
        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        while (_peers.Count > 0 || _punchers.Count > 0)
        {
            try
            {
                if (_udpSocket == null) break;
                if (!_udpSocket.Poll(100_000, SelectMode.SelectRead)) continue;
                int len = _udpSocket.ReceiveFrom(buffer, ref remote);
                if (len <= 1) continue;

                // 발신자 PlayerId 확인
                int senderId = 0;
                foreach (var kv in _peers)
                    if (kv.Value.Equals(remote)) { senderId = kv.Key; break; }

                byte[] copy = new byte[len];
                Buffer.BlockCopy(buffer, 0, copy, 0, len);
                int captured = senderId;
                MainThreadDispatcher.Enqueue(() =>
                {
                    LastSenderPlayerId = captured;
                    PacketManager.HandlePacket(new ArraySegment<byte>(copy));
                    LastSenderPlayerId = 0;
                });
            }
            catch { break; }
        }
    }

    private bool PrepareSocket()
    {
        try
        {
            _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));
            return true;
        }
        catch (Exception e) { HandleFailure(e.Message); return false; }
    }

    private IPEndPoint SelectEndPoint(PeerInfoT info)
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

    public PeerInfoT GetMyPeerInfo() => new PeerInfoT
    {
        PlayerId   = NetworkManager.Instance.MyPlayerId,
        PublicIp   = _myPublicEp.Address.ToString(),
        PublicPort = (ushort)_myPublicEp.Port,
        PrivateIp   = GetLocalPrivateIp(),
        PrivatePort = (ushort)((IPEndPoint)_udpSocket.LocalEndPoint).Port,
    };

    public void CancelP2P()
    {
        foreach (var p in _punchers.Values) p.Stop();
        _punchers.Clear();
        _peers.Clear();
        _udpSocket?.Close();
        _udpSocket = null;
        IsHost = false;
    }

    public void HandleFailure(string msg)
    {
        MainThreadDispatcher.Enqueue(() => OnP2PFailed?.Invoke(msg));
    }

    protected override void OnDestroy()
    {
        foreach (var p in _punchers.Values) p.Stop();
        _udpSocket?.Close();
        base.OnDestroy();
    }
}
