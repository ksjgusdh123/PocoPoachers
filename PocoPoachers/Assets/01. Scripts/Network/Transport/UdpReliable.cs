using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;

public sealed class UdpReliable
{
    const int MaxRetries = 5;
    const long RetryIntervalMs = 200L;
    const int MaxDedupEntries = 256;

    readonly UdpSession _session;
    readonly List<Pending> _pending = new();
    readonly ConcurrentDictionary<string, byte> _received = new();
    readonly Queue<string> _receivedOrder = new();
    uint _nextSeq = 1;

    struct Pending
    {
        public uint Seq;
        public byte[] Payload;
        public IPEndPoint Ep;
        public int RetriesLeft;
        public long NextSendMs;
    }

    public UdpReliable(UdpSession session)
    {
        _session = session;
        _session.OnReliableReceived += HandleReliableReceived;
        _session.OnReliableAckReceived += HandleReliableAck;
    }

    public void Clear()
    {
        _pending.Clear();
        _received.Clear();
        _receivedOrder.Clear();
        _nextSeq = 1;
    }

    public void Unsubscribe()
    {
        if (_session == null) return;
        _session.OnReliableReceived -= HandleReliableReceived;
        _session.OnReliableAckReceived -= HandleReliableAck;
    }

    public void Send(ArraySegment<byte> payload, IPEndPoint ep, long nowMs)
    {
        if (_session == null || ep == null || payload.Count == 0) return;

        byte[] copy = new byte[payload.Count];
        Buffer.BlockCopy(payload.Array, payload.Offset, copy, 0, payload.Count);

        var pending = new Pending
        {
            Seq = _nextSeq++,
            Payload = copy,
            Ep = ep,
            RetriesLeft = MaxRetries,
            NextSendMs = nowMs,
        };
        _pending.Add(pending);
        _session.SendReliable(pending.Seq, new ArraySegment<byte>(copy), ep);
    }

    public void Tick(long nowMs)
    {
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            var p = _pending[i];
            if (nowMs < p.NextSendMs) continue;

            if (p.RetriesLeft <= 0)
            {
                _pending.RemoveAt(i);
                continue;
            }

            p.RetriesLeft--;
            p.NextSendMs = nowMs + RetryIntervalMs;
            _pending[i] = p;
            _session.SendReliable(p.Seq, new ArraySegment<byte>(p.Payload), p.Ep);
        }
    }

    void HandleReliableAck(uint seq, IPEndPoint sender)
    {
        for (int i = _pending.Count - 1; i >= 0; i--)
        {
            if (_pending[i].Seq == seq && _pending[i].Ep.Equals(sender))
            {
                _pending.RemoveAt(i);
                return;
            }
        }
    }

    void HandleReliableReceived(uint seq, ArraySegment<byte> payload, IPEndPoint sender)
    {
        _session.SendAck(seq, sender);

        string key = $"{sender}:{seq}";
        if (!_received.TryAdd(key, 0))
            return;

        _receivedOrder.Enqueue(key);
        while (_receivedOrder.Count > MaxDedupEntries)
        {
            string old = _receivedOrder.Dequeue();
            _received.TryRemove(old, out _);
        }

        byte[] copy = new byte[payload.Count];
        Buffer.BlockCopy(payload.Array, payload.Offset, copy, 0, payload.Count);
        RoomManager.Instance?.DispatchReliablePacket(new ArraySegment<byte>(copy), sender);
    }
}
