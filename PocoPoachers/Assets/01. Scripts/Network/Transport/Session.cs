using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

public class Session
{
    public static readonly int HeaderSize = 2;

    readonly Action<ArraySegment<byte>> _handlePacket;

    Socket _socket = null!;
    int _disconnected = 0;
    public bool IsConnected => _disconnected == 0;

    RecvBuffer _recvBuffer = new RecvBuffer(1024);

    object _lock = new object();
    Queue<ArraySegment<byte>> _sendQueue = new Queue<ArraySegment<byte>>();
    List<ArraySegment<byte>> _pendingList = new List<ArraySegment<byte>>();
    SocketAsyncEventArgs _sendArgs = new SocketAsyncEventArgs();
    SocketAsyncEventArgs _recvArgs = new SocketAsyncEventArgs();

    public Session(Action<ArraySegment<byte>> handlePacket)
    {
        _handlePacket = handlePacket;
    }

    public void OnConnected(EndPoint endPoint)
    {
        MainThreadDispatcher.Enqueue(() => NetworkManager.Instance?.OnSessionConnected());
    }

    public void OnDisconnected(EndPoint endPoint)
    {
        MainThreadDispatcher.Enqueue(() => NetworkManager.Instance?.OnSessionDisconnected());
    }

    public virtual void OnSend(int numOfBytes) { }

    public int OnRecv(ArraySegment<byte> buffer)
    {
        int processLen = 0;

        while (true)
        {
            if (buffer.Count < HeaderSize)
                break;

            ushort dataSize = BitConverter.ToUInt16(buffer.Array!, buffer.Offset);
            if (buffer.Count < dataSize)
                break;

            ProcessPacket(new ArraySegment<byte>(buffer.Array!, buffer.Offset, dataSize));

            processLen += dataSize;
            buffer = new ArraySegment<byte>(buffer.Array!, buffer.Offset + dataSize, buffer.Count - dataSize);
        }

        return processLen;
    }

    void ProcessPacket(ArraySegment<byte> buffer)
    {
        if (buffer.Array == null || buffer.Count <= HeaderSize) return;

        try
        {
            _handlePacket?.Invoke(buffer);
        }
        catch (Exception e)
        {
            Debug.LogError($"OnRecvPacket error: {e}");
            Disconnect();
        }
    }

    public void Start(Socket socket)
    {
        _socket = socket;
        _recvArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnRecvCompleted);
        _sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnSendCompleted);

        RegisterRecv();
    }

    public void Send(ArraySegment<byte> sendBuff)
    {
        lock (_lock)
        {
            _sendQueue.Enqueue(sendBuff);
            if (_pendingList.Count == 0)
                RegisterSend();
        }
    }

    public void Disconnect()
    {
        if (Interlocked.Exchange(ref _disconnected, 1) == 1)
            return;

        Socket socket = _socket;

        // Start() 이전(_socket이 null!)이거나 이미 닫힌 소켓에서는 RemoteEndPoint 접근과
        // Shutdown이 NullReference/SocketException/ObjectDisposedException을 던진다.
        EndPoint remote = null;
        try { remote = socket?.RemoteEndPoint; }
        catch { /* 연결 전이거나 이미 끊긴 상태 — 주소를 알 수 없어도 통지는 해야 한다 */ }

        OnDisconnected(remote);

        try { socket?.Shutdown(SocketShutdown.Both); }
        catch { /* 이미 닫힌 경우 무시 */ }

        try { socket?.Close(); }
        catch { /* 무시 */ }

        // 진행 중인 비동기 I/O의 완료 콜백이 끊긴 세션 상태를 건드리지 않게 핸들러를 떼어둔다.
        // (SocketAsyncEventArgs 자체는 Dispose하지 않는다 — 대기 중인 I/O가 있으면
        //  ObjectDisposedException으로 이어지고, 이 세션은 프로세스당 소수만 생성된다.)
        _recvArgs.Completed -= OnRecvCompleted;
        _sendArgs.Completed -= OnSendCompleted;
    }

    void RegisterSend()
    {
        while (_sendQueue.Count > 0)
        {
            ArraySegment<byte> buff = _sendQueue.Dequeue();
            _pendingList.Add(buff);
        }
        _sendArgs.BufferList = _pendingList;

        bool pending = _socket.SendAsync(_sendArgs);
        if (!pending)
            OnSendCompleted(null, _sendArgs);
    }

    void OnSendCompleted(object sender, SocketAsyncEventArgs args)
    {
        lock (_lock)
        {
            if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
            {
                try
                {
                    int sent = args.BytesTransferred;
                    _sendArgs.BufferList = null;
                    _pendingList.Clear();

                    OnSend(sent);

                    if (_sendQueue.Count > 0)
                        RegisterSend();
                }
                catch (Exception e)
                {
                    Debug.LogError($"[Net] Send Failed: {e}");
                }
            }
            else
            {
                Disconnect();
            }
        }
    }

    void RegisterRecv()
    {
        _recvBuffer.Clean();
        ArraySegment<byte> segment = _recvBuffer.WriteSegment;
        _recvArgs.SetBuffer(segment.Array!, segment.Offset, segment.Count);

        bool pending = _socket.ReceiveAsync(_recvArgs);
        if (!pending)
            OnRecvCompleted(null, _recvArgs);
    }

    void OnRecvCompleted(object sender, SocketAsyncEventArgs args)
    {
        if (args.BytesTransferred > 0 && args.SocketError == SocketError.Success)
        {
            try
            {
                if (_recvBuffer.OnWrite(args.BytesTransferred) == false)
                {
                    Disconnect();
                    return;
                }

                int processLen = OnRecv(_recvBuffer.ReadSegment);
                if (processLen < 0 || _recvBuffer.DataSize < processLen)
                {
                    Disconnect();
                    return;
                }

                if (_recvBuffer.OnRead(processLen) == false)
                {
                    Disconnect();
                    return;
                }

                RegisterRecv();
            }
            catch (Exception e)
            {
                Debug.LogError($"[Net] Recv Failed: {e}");
                RegisterRecv();
            }
        }
        else
        {
            Disconnect();
        }
    }
}

