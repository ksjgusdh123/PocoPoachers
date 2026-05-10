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

    Socket _socket;
    Thread _recvThread;

    public bool Bind()
    {
        try
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _socket.Bind(new IPEndPoint(IPAddress.Any, 0));
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
        _recvThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "Udp-Recv" };
        _recvThread.Start();
    }

    public void Send(ArraySegment<byte> data, IPEndPoint ep)
    {
        if (_socket == null) return;
        try { _socket.SendTo(data.Array, data.Offset, data.Count, SocketFlags.None, ep); }
        catch (Exception e) { Debug.LogWarning($"[UdpSession] Send to {ep} failed: {e.Message}"); }
    }

    public void Close()
    {
        _socket?.Close();
        _socket = null;
    }

    void ReceiveLoop()
    {
        byte[] buffer = new byte[2048];
        EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
        while (_socket != null)
        {
            try
            {
                if (!_socket.Poll(100_000, SelectMode.SelectRead)) continue;
                int len = _socket.ReceiveFrom(buffer, ref remote);
                if (len <= 1) continue;

                byte[] copy = new byte[len];
                Buffer.BlockCopy(buffer, 0, copy, 0, len);
                OnReceived?.Invoke(new ArraySegment<byte>(copy), (IPEndPoint)remote);
            }
            catch { break; }
        }
    }
}
