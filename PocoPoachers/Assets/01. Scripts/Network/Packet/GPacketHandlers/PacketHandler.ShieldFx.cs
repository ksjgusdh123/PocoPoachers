public static partial class PacketHandlers
{
    // 게스트의 방어막 연출 요청 — 데미지 면역 판정(G_Invincible)과 무관한 순수 연출이라
    // 호스트도 자기 화면의 그 게스트 옆에 방어막을 띄운 뒤, 나머지 게스트에게 중계한다(보낸 게스트는 스킵).
    public static void OnG_ShieldFx(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId)) return;

        var packet = root.TypeAsG_ShieldFx();

        ShieldFxVisual.SetActiveFor(guestId, packet.Active);
        RoomSync.ShieldFxRelay(guestId, packet.Active);
    }
}
