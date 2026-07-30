using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class UdpSession
{
    public IPEndPoint LocalEndPoint => (_socket?.LocalEndPoint as IPEndPoint)!;
    public Socket Socket => _socket;

    public event Action<ArraySegment<byte>, IPEndPoint> OnReceived;
    public event Action<IPEndPoint> OnPunchReceived;
    public event Action<IPEndPoint> OnKeepaliveReceived;

    public const byte PunchSignal = 0x01;
    public const byte KeepaliveSignal = 0x02;
    public const byte ReliableSignal = 0x03;
    public const byte AckSignal = 0x04;
    public const int ReliableHeaderSize = 5;

    static readonly byte[] KeepalivePayload = { KeepaliveSignal };

    Socket _socket;
    Thread _recvThread;
    volatile bool _running;

    public event Action<uint, ArraySegment<byte>, IPEndPoint> OnReliableReceived;
    public event Action<uint, IPEndPoint> OnReliableAckReceived;

    public bool Bind()
    {
        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.Bind(new IPEndPoint(IPAddress.Any, 0));
            try
            {
                // Windows: ICMP Port Unreachable를 SocketException으로 올리지 않도록 비활성화
                const int SIO_UDP_CONNRESET = unchecked((int)0x9800000C);
                _socket.IOControl(SIO_UDP_CONNRESET, new byte[] { 0, 0, 0, 0 }, null);
            }
            catch { /* Windows 전용, 다른 플랫폼은 무시 */ }
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[UdpSession] Bind failed: {e.Message}");
            return false;
        }
    }

    public void StartReceive()
    {
        if (_recvThread?.IsAlive ?? false) return;
        _running = true;
        _recvThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "Udp-Recv" };
        _recvThread.Start();
    }

    public void Send(ArraySegment<byte> data, IPEndPoint ep)
    {
        if (_socket == null) return;
        try { _socket.SendTo(data.Array, data.Offset, data.Count, SocketFlags.None, ep); }
        catch (Exception e) { Debug.LogWarning($"[UdpSession] Send to {ep} failed: {e.Message}"); }
    }

    public void SendKeepalive(IPEndPoint ep) => Send(new ArraySegment<byte>(KeepalivePayload), ep);

    public void SendReliable(uint seq, ArraySegment<byte> payload, IPEndPoint ep)
    {
        if (_socket == null || payload.Count == 0) return;
        byte[] buffer = new byte[ReliableHeaderSize + payload.Count];
        buffer[0] = ReliableSignal;
        BitConverter.GetBytes(seq).CopyTo(buffer, 1);
        Buffer.BlockCopy(payload.Array, payload.Offset, buffer, ReliableHeaderSize, payload.Count);
        Send(new ArraySegment<byte>(buffer), ep);
    }

    public void SendAck(uint seq, IPEndPoint ep)
    {
        byte[] buffer = new byte[ReliableHeaderSize];
        buffer[0] = AckSignal;
        BitConverter.GetBytes(seq).CopyTo(buffer, 1);
        Send(new ArraySegment<byte>(buffer), ep);
    }

    public void Close()
    {
        _running = false;

        // 소켓을 먼저 닫아 Poll에서 대기 중인 수신 스레드를 깨운다.
        Socket socket = _socket;
        _socket = null;
        socket?.Close();

        Thread thread = _recvThread;
        _recvThread = null;

        // 스레드를 회수하지 않으면 재연결마다 좀비 스레드가 누적된다.
        // 수신 스레드 자신이 Close를 호출하는 경우의 자기 Join(데드락)은 피한다.
        if (thread != null && thread != Thread.CurrentThread && thread.IsAlive)
            thread.Join(300);
    }

    void ReceiveLoop()
    {
        byte[] buffer = new byte[2048];
        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        while (_running)
        {
            // Close()가 _socket을 null로 바꾸는 것과 겹치지 않도록 지역 변수로 고정한다.
            Socket socket = _socket;
            if (socket == null) break;

            try
            {
                if (!socket.Poll(100_000, SelectMode.SelectRead)) continue;
                int len = socket.ReceiveFrom(buffer, ref remote);
                if (len <= 0) continue;

                var ep = (IPEndPoint)remote;
                if (len == 1 && buffer[0] == PunchSignal)
                {
                    OnPunchReceived?.Invoke(ep);
                    continue;
                }
                if (len == 1 && buffer[0] == KeepaliveSignal)
                {
                    OnKeepaliveReceived?.Invoke(ep);
                    continue;
                }
                if (len == ReliableHeaderSize && buffer[0] == AckSignal)
                {
                    OnReliableAckReceived?.Invoke(BitConverter.ToUInt32(buffer, 1), ep);
                    continue;
                }
                if (len > ReliableHeaderSize && buffer[0] == ReliableSignal)
                {
                    uint seq = BitConverter.ToUInt32(buffer, 1);
                    byte[] reliablePayload = new byte[len - ReliableHeaderSize];
                    Buffer.BlockCopy(buffer, ReliableHeaderSize, reliablePayload, 0, reliablePayload.Length);
                    OnReliableReceived?.Invoke(seq, new ArraySegment<byte>(reliablePayload), ep);
                    continue;
                }

                byte[] copy = new byte[len];
                Buffer.BlockCopy(buffer, 0, copy, 0, len);
                OnReceived?.Invoke(new ArraySegment<byte>(copy), ep);
            }
            catch (Exception) { if (!_running || _socket == null) break; }
        }
    }
}
