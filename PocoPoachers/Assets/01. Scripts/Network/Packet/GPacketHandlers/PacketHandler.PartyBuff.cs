public static partial class PacketHandlers
{
    // 게스트의 팀원 버프 오라 켜짐/꺼짐 요청 — "누가 범위 안에 있는지" 판정은 각자 로컬에서 하므로
    // 호스트는 등록(PartyBuffRegistry) + 나머지 게스트에게 중계만 한다.
    public static void OnG_PartyBuff(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId))
            return;

        var packet = root.TypeAsG_PartyBuff();

        PartyBuffRegistry.SetActive(guestId, packet.SkillId, packet.Active);
        RoomSync.PartyBuffRelay(guestId, packet.SkillId, packet.Active);
    }
}
