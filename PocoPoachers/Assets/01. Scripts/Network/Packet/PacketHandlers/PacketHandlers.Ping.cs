using System;

public static partial class PacketHandlers
{
    public static void OnS_HeartbeatAck(FlatPacket root)
    {
        var pkt = root.TypeAsS_HeartbeatAck();
        long rtt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - pkt.SendTime;
        MainThreadDispatcher.Enqueue(() => NetworkManager.Instance?.OnPongRes(rtt));
    }
}
