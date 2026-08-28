public static partial class PacketHandlers
{
    // 다른 플레이어(또는 나를 대신 중계한 호스트)의 팀원 버프 오라 상태 통보 — 등록만 한다.
    // "누가 범위 안에 있는지" 판정과 오라 연출은 이 클라이언트의 PartyBuffReceiver가 매 틱 알아서
    // 계산하므로(나 자신 포함, 화면에 보이는 모든 플레이어) 여기서 직접 연출을 건드릴 필요가 없다.
    public static void OnH_PartyBuff(FlatPacket root)
    {
        var packet = root.TypeAsH_PartyBuff();

        var nm = NetworkManager.Instance;
        if (nm != null && packet.PlayerId == nm.MyPlayerId) return;

        PartyBuffRegistry.SetActive(packet.PlayerId, packet.SkillId, packet.Active);
    }
}
