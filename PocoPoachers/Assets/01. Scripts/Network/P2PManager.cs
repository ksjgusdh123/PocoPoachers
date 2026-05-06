using System;
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

    public bool IsHost        { get; private set; }
    public bool IsConnected   { get; private set; }
    public IPEndPoint PeerEp  { get; private set; }

    public event Action          OnP2PConnected;
    public event Action<string>  OnP2PFailed;

    Socket          _udpSocket;
    IPEndPoint      _myPublicEp;
    UdpHolePuncher  _puncher;
    Thread          _recvThread;

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
        IsConnected = false;

        ThreadPool.QueueUserWorkItem(_ => {
            if (!PrepareSocket()) return;

            // STUN으로 공인 IP 식별
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
        PeerEp = SelectEndPoint(target);

        _puncher = new UdpHolePuncher(_udpSocket);
        _puncher.OnSuccess += _ => {
            IsConnected = true;
            _recvThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "P2P-Recv" };
            _recvThread.Start();
            MainThreadDispatcher.Enqueue(() => OnP2PConnected?.Invoke());
        };
        _puncher.OnFailed += (p, reason) => HandleFailure(reason);

        _puncher.Start(PeerEp);
    }

    public void SendP2P<TTable, TObj>(TObj data, Func<FlatBufferBuilder, TObj, Offset<TTable>> packFunc, PacketType type)
        where TTable : struct where TObj : class
    {
        if (!IsConnected || _udpSocket == null || PeerEp == null) return;
        try
        {
            var segment = PacketBuilder.BuildSegment(data, packFunc, type);
            _udpSocket.SendTo(segment.Array, segment.Offset, segment.Count, SocketFlags.None, PeerEp);
        }
        catch (Exception e) { Debug.LogWarning($"[P2P] Send failed: {e.Message}"); }
    }

    private void ReceiveLoop()
    {
        byte[] buffer = new byte[2048];
        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        while (IsConnected)
        {
            try
            {
                if (_udpSocket == null) break;
                if (!_udpSocket.Poll(100_000, SelectMode.SelectRead)) continue;
                int len = _udpSocket.ReceiveFrom(buffer, ref remote);
                if (len <= 1) continue;
                byte[] copy = new byte[len];
                Buffer.BlockCopy(buffer, 0, copy, 0, len);
                MainThreadDispatcher.Enqueue(() => PacketManager.HandlePacket(new ArraySegment<byte>(copy)));
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
        PlayerId = NetworkManager.Instance.MyPlayerId,
        PublicIp = _myPublicEp.Address.ToString(),
        PublicPort = (ushort)_myPublicEp.Port,
        PrivateIp = GetLocalPrivateIp(),
        PrivatePort = (ushort)((IPEndPoint)_udpSocket.LocalEndPoint).Port
    };

    public void CancelP2P()
    {
        _puncher?.Stop();
        _puncher = null;
        _udpSocket?.Close();
        _udpSocket = null;
        IsConnected = false;
        IsHost      = false;
    }

    public void HandleFailure(string msg)
    {
        MainThreadDispatcher.Enqueue(() => OnP2PFailed?.Invoke(msg));
    }

    protected override void OnDestroy()
    {
        _puncher?.Stop();
        _udpSocket?.Close();
        base.OnDestroy();
    }
}
