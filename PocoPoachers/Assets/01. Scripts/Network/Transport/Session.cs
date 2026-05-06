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
        MainThreadDispatcher.Enqueue(() => NetworkManager.Instance?.NotifySessionConnected());
    }

    public void OnDisconnected(EndPoint endPoint)
    {
        MainThreadDispatcher.Enqueue(() => NetworkManager.Instance?.NotifySessionDisconnected());
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

        OnDisconnected(_socket.RemoteEndPoint);
        _socket.Shutdown(SocketShutdown.Both);
        _socket.Close();
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
