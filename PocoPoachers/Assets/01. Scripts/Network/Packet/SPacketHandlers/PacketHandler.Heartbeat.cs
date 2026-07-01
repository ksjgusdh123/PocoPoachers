
using System;

public static partial class PacketHandlers
{
    public static void OnS_HeartbeatAck(FlatPacket root)
    {
        var packet = root.TypeAsS_HeartbeatAck();
        long rtt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - packet.SendTime;
        MainThreadDispatcher.Enqueue(() => NetworkManager.Instance?.OnHeartbeatAck(rtt));
    }
}
