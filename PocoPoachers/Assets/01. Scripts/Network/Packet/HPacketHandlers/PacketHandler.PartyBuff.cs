public static partial class PacketHandlers
{
    // 다른 플레이어(또는 나를 대신 중계한 호스트)의 팀원 버프 오라 상태 통보 — 등록만 한다.
    // 내 것은 이미 로컬에 등록돼 있고, 호스트가 나를 뺀 나머지에게만 중계하므로 보통 자기 것은 오지 않는다.
    public static void OnH_PartyBuff(FlatPacket root)
    {
        var packet = root.TypeAsH_PartyBuff();

        var nm = NetworkManager.Instance;
        if (nm != null && packet.PlayerId == nm.MyPlayerId) return;

        PartyBuffRegistry.SetActive(packet.PlayerId, packet.SkillId, packet.Active);
    }
}
