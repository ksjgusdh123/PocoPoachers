public static partial class PacketHandlers
{
    public static void OnG_Rescue(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int rescuerId))
            return;

        var packet = root.TypeAsG_Rescue();

        // 구출자 id는 패킷 내용이 아니라 송신자로 판별한다 — 남을 사칭한 구출 알림 방지
        RescueRelay.Relay(rescuerId, packet.TargetId, (RescueState)packet.State, packet.Duration);
    }
}
