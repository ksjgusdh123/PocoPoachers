using System;
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

    // 수신 중복 제거 캐시. OnReliablePacketReceived는 UdpSession의 수신 스레드에서 호출되고
    // Clear()는 메인 스레드에서 호출되므로 두 컨테이너를 하나의 lock으로 함께 보호해야 한다.
    // (이전에는 Queue가 무보호 상태여서 게스트 2명의 패킷이 동시 도착하면 자료구조가 깨졌다.)
    // 키는 값 튜플 — $"{sender}:{sequence}" 문자열 보간은 신뢰 패킷마다 문자열을 할당한다.
    readonly object _receivedLock = new();
    readonly Dictionary<(IPEndPoint Sender, uint Sequence), byte> _receivedKeys = new();
    readonly Queue<(IPEndPoint Sender, uint Sequence)> _receivedKeyOrder = new();
    uint _nextSequence = 1;

    public event Action<PacketType, IPEndPoint> OnDeliveryFailed;

    struct PendingSend
    {
        public uint Sequence;
        public PacketType Type;
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
        lock (_receivedLock)
        {
            _receivedKeys.Clear();
            _receivedKeyOrder.Clear();
        }
    }

    public void Unsubscribe()
    {
        if (_session == null) return;
        _session.OnReliableReceived -= OnReliablePacketReceived;
        _session.OnReliableAckReceived -= OnReliableAck;
    }

    public void Send(ArraySegment<byte> payload, IPEndPoint endPoint, long nowMs, PacketType type = PacketType.NONE)
    {
        if (_session == null || endPoint == null || payload.Count == 0) return;

        byte[] copy = new byte[payload.Count];
        Buffer.BlockCopy(payload.Array, payload.Offset, copy, 0, payload.Count);

        var pending = new PendingSend
        {
            Sequence = 0,
            Type = type,
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
        List<(PacketType Type, IPEndPoint EndPoint)> failed = null;

        lock (_pendingLock)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                var p = _pending[i];
                if (nowMs < p.NextSendMs) continue;

                if (p.RemainingRetries <= 0)
                {
                    (failed ??= new()).Add((p.Type, p.EndPoint));
                    _pending.RemoveAt(i);
                    continue;
                }

                p.RemainingRetries--;
                p.NextSendMs = nowMs + RetryIntervalMs;
                _pending[i] = p;
                _session.SendReliable(p.Sequence, new ArraySegment<byte>(p.Payload), p.EndPoint);
            }
        }

        if (failed != null)
            foreach (var (type, endPoint) in failed)
                OnDeliveryFailed?.Invoke(type, endPoint);
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

        var key = (Sender: sender, Sequence: sequence);
        lock (_receivedLock)
        {
            if (_receivedKeys.ContainsKey(key))
                return;

            _receivedKeys[key] = 0;
            _receivedKeyOrder.Enqueue(key);
            while (_receivedKeyOrder.Count > MaxReceivedCache)
            {
                var old = _receivedKeyOrder.Dequeue();
                _receivedKeys.Remove(old);
            }
        }

        byte[] copy = new byte[payload.Count];
        Buffer.BlockCopy(payload.Array, payload.Offset, copy, 0, payload.Count);
        RoomManager.Instance?.HandleReliablePacket(new ArraySegment<byte>(copy), sender);
    }
}
