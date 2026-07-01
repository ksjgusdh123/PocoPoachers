using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

public sealed class UdpReliable
{
    const int MaxRetries = 5;
    const long RetryIntervalMs = 200L;
    const int MaxReceivedCache = 256;

    readonly UdpSession _session;
    readonly object _pendingLock = new();
    readonly List<PendingSend> _pending = new();
    readonly ConcurrentDictionary<string, byte> _receivedKeys = new();
    readonly Queue<string> _receivedKeyOrder = new();
    uint _nextSequence = 1;

    struct PendingSend
    {
        public uint Sequence;
        public byte[] Payload;
        public IPEndPoint EndPoint;
        public int RemainingRetries;
        public long NextSendMs;
    }

    public UdpReliable(UdpSession session)
    {
        _session = session;
        _session.OnReliableReceived += OnReliablePacketReceived;
        _session.OnReliableAckReceived += OnReliableAck;
    }

    public void Clear()
    {
        lock (_pendingLock)
        {
            _pending.Clear();
            _nextSequence = 1;
        }
        _receivedKeys.Clear();
        _receivedKeyOrder.Clear();
    }

    public void Unsubscribe()
    {
        if (_session == null) return;
        _session.OnReliableReceived -= OnReliablePacketReceived;
        _session.OnReliableAckReceived -= OnReliableAck;
    }

    public void Send(ArraySegment<byte> payload, IPEndPoint endPoint, long nowMs)
    {
        if (_session == null || endPoint == null || payload.Count == 0) return;

        byte[] copy = new byte[payload.Count];
        Buffer.BlockCopy(payload.Array, payload.Offset, copy, 0, payload.Count);

        var pending = new PendingSend
        {
            Sequence = 0,
            Payload = copy,
            EndPoint = endPoint,
            RemainingRetries = MaxRetries,
            NextSendMs = nowMs,
        };
        lock (_pendingLock)
        {
            pending.Sequence = _nextSequence++;
            _pending.Add(pending);
        }
        _session.SendReliable(pending.Sequence, new ArraySegment<byte>(copy), endPoint);
    }

    public void Tick(long nowMs)
    {
        lock (_pendingLock)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                var p = _pending[i];
                if (nowMs < p.NextSendMs) continue;

                if (p.RemainingRetries <= 0)
                {
                    _pending.RemoveAt(i);
                    continue;
                }

                p.RemainingRetries--;
                p.NextSendMs = nowMs + RetryIntervalMs;
                _pending[i] = p;
                _session.SendReliable(p.Sequence, new ArraySegment<byte>(p.Payload), p.EndPoint);
            }
        }
    }

    void OnReliableAck(uint sequence, IPEndPoint sender)
    {
        lock (_pendingLock)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                if (_pending[i].Sequence == sequence && _pending[i].EndPoint.Equals(sender))
                {
                    _pending.RemoveAt(i);
                    return;
                }
            }
        }
    }

    void OnReliablePacketReceived(uint sequence, ArraySegment<byte> payload, IPEndPoint sender)
    {
        _session.SendAck(sequence, sender);

        string key = $"{sender}:{sequence}";
        if (!_receivedKeys.TryAdd(key, 0))
            return;

        _receivedKeyOrder.Enqueue(key);
        while (_receivedKeyOrder.Count > MaxReceivedCache)
        {
            string old = _receivedKeyOrder.Dequeue();
            _receivedKeys.TryRemove(old, out _);
        }

        byte[] copy = new byte[payload.Count];
        Buffer.BlockCopy(payload.Array, payload.Offset, copy, 0, payload.Count);
        RoomManager.Instance?.HandleReliablePacket(new ArraySegment<byte>(copy), sender);
    }
}
