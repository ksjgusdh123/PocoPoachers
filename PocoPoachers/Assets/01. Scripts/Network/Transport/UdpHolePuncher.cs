using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

// UDP Hole Punching: 양쪽이 서로의 공개 엔드포인트로 UDP 패킷을 반복 전송해
// NAT에 구멍을 뚫고 직접 연결을 수립한다.
public class UdpHolePuncher
{
    public enum State { Idle, Punching, Success, Failed }

    // 첫 바이트로 패킷 종류 구분
    const byte PunchByte   = 0xAA;
    const byte AckByte     = 0xBB;
    const int  SendIntervalMs = 500;
    const int  TimeoutMs      = 10_000;

    public State CurrentState { get; private set; } = State.Idle;
    public IPEndPoint LocalEndPoint  { get; private set; }

    // volatile: RecvLoop(recv thread)에서 갱신, SendLoop(send thread)에서 읽음
    volatile IPEndPoint _remoteEp;
    public IPEndPoint RemoteEndPoint => _remoteEp;

    public event Action<UdpHolePuncher>          OnSuccess;
    public event Action<UdpHolePuncher, string>  OnFailed;

    Socket    _socket;
    Thread    _sendThread;
    Thread    _recvThread;
    CancellationTokenSource _cts;
    bool      _ackSent;

    // localPort: STUN에서 열어둔 포트와 동일해야 NAT 매핑이 유지된다.
    public void Start(IPEndPoint remoteEp, int localPort)
    {
        if (CurrentState != State.Idle) return;

        _remoteEp    = remoteEp;
        CurrentState = State.Punching;
        _cts           = new CancellationTokenSource();

        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _socket.Bind(new IPEndPoint(IPAddress.Any, localPort));
        LocalEndPoint = (IPEndPoint)_socket.LocalEndPoint;

        _recvThread = new Thread(RecvLoop) { IsBackground = true, Name = "HolePunch.Recv" };
        _sendThread = new Thread(SendLoop) { IsBackground = true, Name = "HolePunch.Send" };
        _recvThread.Start();
        _sendThread.Start();

        // 전체 타임아웃
        _cts.Token.Register(() => { });
        ThreadPool.QueueUserWorkItem(_ =>
        {
            Thread.Sleep(TimeoutMs);
            if (CurrentState == State.Punching) Fail("Timeout");
        });

        Debug.Log($"[HolePuncher] Start punching → {remoteEp}");
    }

    public void Stop()
    {
        _cts?.Cancel();
        _socket?.Close();
    }

    void SendLoop()
    {
        byte[] punch = { PunchByte };
        byte[] ack   = { AckByte };
        var token = _cts.Token;

        while (!token.IsCancellationRequested && CurrentState == State.Punching)
        {
            try
            {
                _socket.SendTo(punch, RemoteEndPoint);
                if (_ackSent) _socket.SendTo(ack, RemoteEndPoint);
            }
            catch { break; }
            Thread.Sleep(SendIntervalMs);
        }
    }

    void RecvLoop()
    {
        byte[] buf = new byte[16];
        var token  = _cts.Token;
        _socket.ReceiveTimeout = 1000;

        while (!token.IsCancellationRequested && CurrentState == State.Punching)
        {
            try
            {
                EndPoint remoteEp = new IPEndPoint(IPAddress.Any, 0);
                int n = _socket.ReceiveFrom(buf, ref remoteEp);
                if (n < 1) continue;

                if (buf[0] == PunchByte)
                {
                    // 실제 수신 주소로 타겟 갱신: 서버가 전달한 포트가 틀렸을 때 교정
                    var actual = (IPEndPoint)remoteEp;
                    if (!actual.Equals(_remoteEp))
                    {
                        Debug.Log($"[HolePuncher] RemoteEP 교정: {_remoteEp} → {actual}");
                        _remoteEp = actual;
                    }
                    _ackSent = true;
                }
                else if (buf[0] == AckByte)
                {
                    // 상대가 내 punch를 받았다 = 양방향 개통
                    Succeed();
                    return;
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                // recv 타임아웃은 정상 — 루프 계속
            }
            catch { break; }
        }
    }

    void Succeed()
    {
        if (CurrentState != State.Punching) return;
        CurrentState = State.Success;
        _cts.Cancel();
        Debug.Log($"[HolePuncher] Success ↔ {RemoteEndPoint}");
        MainThreadDispatcher.Enqueue(() => OnSuccess?.Invoke(this));
    }

    void Fail(string reason)
    {
        if (CurrentState != State.Punching) return;
        CurrentState = State.Failed;
        _cts.Cancel();
        _socket?.Close();
        Debug.LogWarning($"[HolePuncher] Failed: {reason}");
        MainThreadDispatcher.Enqueue(() => OnFailed?.Invoke(this, reason));
    }
}
