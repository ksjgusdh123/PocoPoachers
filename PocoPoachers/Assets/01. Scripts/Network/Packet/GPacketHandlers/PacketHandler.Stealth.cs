public static partial class PacketHandlers
{
    // 게스트의 은신 요청 — 탐지는 호스트만 판정하므로 호스트의 RemotePlayerStat에 반영하고,
    // 호스트 화면의 반투명 연출도 같이 켠 뒤, 나머지 게스트에게 중계한다(보낸 게스트는 스킵).
    public static void OnG_Stealth(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId))
            return;

        var packet = root.TypeAsG_Stealth();

        if (ObjectManager.Instance != null && ObjectManager.Instance.TryGet(ObjectKind.Player, guestId, out var playerObj))
        {
            if (playerObj.TryGetComponent<StatBase>(out var stat))
                stat.ApplyStealthFromNetwork(packet.Active);

            StealthVisual.SetActiveForSelf(playerObj.gameObject, packet.Active, packet.Alpha);
        }

        RoomSync.StealthRelay(guestId, packet.Active, packet.Alpha);
    }
}
