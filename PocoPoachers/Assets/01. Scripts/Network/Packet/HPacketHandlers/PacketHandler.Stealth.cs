public static partial class PacketHandlers
{
    // 다른 플레이어(또는 나를 대신 중계한 호스트)의 은신 상태 통보 — 연출만 재생한다.
    // 내 은신은 이미 로컬에 적용돼 있고, 호스트가 나를 뺀 나머지에게만 중계하므로 보통 자기 것은 오지 않는다.
    public static void OnH_Stealth(FlatPacket root)
    {
        var packet = root.TypeAsH_Stealth();

        var nm = NetworkManager.Instance;
        if (nm != null && packet.PlayerId == nm.MyPlayerId) return;

        StealthVisual.SetActiveFor(packet.PlayerId, packet.Active, packet.Alpha);
    }
}
