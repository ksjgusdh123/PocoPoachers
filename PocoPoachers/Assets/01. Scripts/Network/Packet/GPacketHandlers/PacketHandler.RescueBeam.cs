public static partial class PacketHandlers
{
    public static void OnG_RescueBeamPlay(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int playerId))
            return;

        // 죽은 플레이어 id는 패킷 내용이 아니라 송신자로 판별한다 — 남을 사칭한 연출 트리거 방지
        PlayerDeathTracker.MarkFinalized(playerId);
        RoomSync.BroadcastRescueBeamPlay(playerId);
    }
}
