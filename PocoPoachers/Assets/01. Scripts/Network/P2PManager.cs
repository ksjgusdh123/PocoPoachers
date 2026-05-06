using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

// P2P Friend Mode 전체 흐름 조율:
//   Host:  STUN → C_RegisterP2PSession → [대기] → S_PeerJoined → UdpHolePuncher
//   Peer:  C_JoinP2PSession → S_P2PSessionInfo → UdpHolePuncher
public class P2PManager : Singleton<P2PManager>
{
    [Header("STUN")]
    [SerializeField] string stunHost = "stun.l.google.com";
    [SerializeField] int    stunPort = 19302;

    [Header("P2P UDP")]
    [SerializeField] int localUdpPort = 0; // 0 = OS가 자동 배정 후 STUN에서 확인

    public bool IsHost        { get; private set; }
    public bool IsConnected   { get; private set; }
    public IPEndPoint PeerEp  { get; private set; }

    public event Action          OnP2PConnected;
    public event Action<string>  OnP2PFailed;

    Socket          _udpSocket;
    IPEndPoint      _publicEp;
    UdpHolePuncher  _puncher;

    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }

    // ── Host side ─────────────────────────────────────────────────────────

    public void StartAsHost(string sessionCode)
    {
        IsHost = true;
        ThreadPool.QueueUserWorkItem(_ => DiscoverAndRegister(sessionCode));
    }

    void DiscoverAndRegister(string sessionCode)
    {
        _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _udpSocket.Bind(new IPEndPoint(IPAddress.Any, localUdpPort));

        if (!StunClient.TryGetPublicEndPoint(stunHost, stunPort, _udpSocket, out _publicEp))
        {
            MainThreadDispatcher.Enqueue(() => OnP2PFailed?.Invoke("STUN 실패"));
            _udpSocket.Close();
            return;
        }

        string privateIp = GetLocalPrivateIp();
        int    privatePort = ((IPEndPoint)_udpSocket.LocalEndPoint).Port;

        Debug.Log($"[P2PManager] Public={_publicEp}  Private={privateIp}:{privatePort}");

        MainThreadDispatcher.Enqueue(() =>
        {
            PacketBuilder.Send(
                new C_RegisterP2PSessionT
                {
                    SessionCode = sessionCode,
                    PublicIp    = _publicEp.Address.ToString(),
                    PublicPort  = (ushort)_publicEp.Port,
                    PrivateIp   = privateIp,
                    PrivatePort = (ushort)privatePort,
                },
                C_RegisterP2PSession.Pack,
                PacketType.C_RegisterP2PSession);
        });
    }

    // 서버가 S_RegisterP2PSessionRes를 보내면 PacketHandlers에서 호출
    public void OnRegistered(bool success)
    {
        if (!success)
        {
            OnP2PFailed?.Invoke("세션 코드 중복");
            _udpSocket?.Close();
        }
        // 성공이면 S_PeerJoined 대기
    }

    // 서버가 S_PeerJoined를 보내면 PacketHandlers에서 호출
    public void OnPeerJoined(string peerPublicIp, ushort peerPublicPort,
                             string peerPrivateIp, ushort peerPrivatePort)
    {
        Debug.Log($"[P2PManager] S_PeerJoined: Public={peerPublicIp}:{peerPublicPort}  Private={peerPrivateIp}:{peerPrivatePort}");
        var target = IsOnSameLan(peerPrivateIp)
            ? new IPEndPoint(IPAddress.Parse(peerPrivateIp), peerPrivatePort)
            : new IPEndPoint(IPAddress.Parse(peerPublicIp),  peerPublicPort);
        BeginPunch(target);
    }

    // ── Peer side ─────────────────────────────────────────────────────────

    public void StartAsPeer(string sessionCode)
    {
        IsHost = false;
        ThreadPool.QueueUserWorkItem(_ => StunAndJoin(sessionCode));
    }

    // Host의 DiscoverAndRegister와 동일한 흐름: STUN으로 공개 EP 확인 후 서버에 Join
    void StunAndJoin(string sessionCode)
    {
        _udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _udpSocket.Bind(new IPEndPoint(IPAddress.Any, 0));

        if (!StunClient.TryGetPublicEndPoint(stunHost, stunPort, _udpSocket, out _publicEp))
        {
            MainThreadDispatcher.Enqueue(() => OnP2PFailed?.Invoke("STUN 실패"));
            _udpSocket.Close();
            _udpSocket = null;
            return;
        }

        string privateIp   = GetLocalPrivateIp();
        int    privatePort = ((IPEndPoint)_udpSocket.LocalEndPoint).Port;
        Debug.Log($"[P2PManager] Peer Public={_publicEp}  Private={privateIp}:{privatePort}");

        MainThreadDispatcher.Enqueue(() =>
            PacketBuilder.Send(
                new C_JoinP2PSessionT
                {
                    SessionCode = sessionCode,
                    PublicIp    = _publicEp.Address.ToString(),
                    PublicPort  = (ushort)_publicEp.Port,
                    PrivateIp   = privateIp,
                    PrivatePort = (ushort)privatePort,
                },
                C_JoinP2PSession.Pack,
                PacketType.C_JoinP2PSession));
    }

    // 서버가 S_P2PSessionInfo를 보내면 PacketHandlers에서 호출
    public void OnSessionInfo(string hostPublicIp, ushort hostPublicPort,
                              string hostPrivateIp, ushort hostPrivatePort)
    {
        Debug.Log($"[P2PManager] S_P2PSessionInfo: Host Public={hostPublicIp}:{hostPublicPort}  Private={hostPrivateIp}:{hostPrivatePort}");
        Debug.Log($"[P2PManager] My public (STUN) = {_publicEp}  ← 서버가 S_PeerJoined에 이 값을 보내야 함");

        var target = IsOnSameLan(hostPrivateIp)
            ? new IPEndPoint(IPAddress.Parse(hostPrivateIp), hostPrivatePort)
            : new IPEndPoint(IPAddress.Parse(hostPublicIp),  hostPublicPort);

        // _udpSocket은 StunAndJoin에서 이미 STUN 완료 — 새로 만들면 NAT 매핑이 깨진다
        BeginPunch(target);
    }

    // ── Hole Punching ──────────────────────────────────────────────────────

    void BeginPunch(IPEndPoint remoteEp)
    {
        PeerEp   = remoteEp;
        _puncher = new UdpHolePuncher();
        _puncher.OnSuccess += HandlePunchSuccess;
        _puncher.OnFailed  += HandlePunchFailed;

        int localPort = ((IPEndPoint)_udpSocket.LocalEndPoint).Port;
        _udpSocket.Close(); // UdpHolePuncher가 같은 포트를 재점유

        _puncher.Start(remoteEp, localPort);
    }

    void HandlePunchSuccess(UdpHolePuncher puncher)
    {
        IsConnected = true;
        Debug.Log($"[P2PManager] P2P 연결 성공 ↔ {PeerEp}");
        // puncher.LocalEndPoint / RemoteEndPoint를 이용해 게임 UDP 소켓 구성
        OnP2PConnected?.Invoke();
    }

    void HandlePunchFailed(UdpHolePuncher puncher, string reason)
    {
        Debug.LogWarning($"[P2PManager] Hole punch 실패 ({reason}) → TURN 폴백 예정");
        OnP2PFailed?.Invoke(reason);
    }

    // ── Cancel ────────────────────────────────────────────────────────────

    public void CancelP2P()
    {
        _puncher?.Stop();
        _puncher = null;
        _udpSocket?.Close();
        _udpSocket = null;
        IsConnected = false;
        IsHost      = false;
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    static string GetLocalPrivateIp()
    {
        using var tmp = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        tmp.Connect("8.8.8.8", 80);
        return ((IPEndPoint)tmp.LocalEndPoint).Address.ToString();
    }

    static bool IsOnSameLan(string remotePrivateIp)
    {
        string local = GetLocalPrivateIp();
        // 서브넷 /24 비교 (단순 근사)
        int lastDot = local.LastIndexOf('.');
        return lastDot > 0 && remotePrivateIp.StartsWith(local[..lastDot]);
    }

    protected override void OnDestroy()
    {
        _puncher?.Stop();
        _udpSocket?.Close();
        base.OnDestroy();
    }
}
