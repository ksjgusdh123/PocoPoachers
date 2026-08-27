public static partial class PacketHandlers
{
    // ReflectFxPrefabPath는 GPacketHandlers/PacketHandler.ReflectFx.cs에 선언되어 있다 —
    // partial class라 같은 클래스라서 여기서도 그대로 쓸 수 있다(중복 선언 금지).

    // 다른 플레이어(또는 나를 대신 중계한 호스트)의 반사 방어막 상태 통보 — 연출만 재생한다.
    // 내 방어막은 이미 로컬에 적용돼 있고, 호스트가 나를 뺀 나머지에게만 중계하므로 보통 자기 것은 오지 않는다.
    public static void OnH_ReflectFx(FlatPacket root)
    {
        var packet = root.TypeAsH_ReflectFx();

        var nm = NetworkManager.Instance;
        if (nm != null && packet.PlayerId == nm.MyPlayerId) return;

        ShieldFxVisual.SetActiveFor(packet.PlayerId, ReflectFxPrefabPath, packet.Active);
    }
}
