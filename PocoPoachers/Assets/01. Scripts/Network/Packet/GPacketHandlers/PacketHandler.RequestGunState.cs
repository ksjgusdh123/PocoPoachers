public static partial class PacketHandlers
{
    // 게스트가 인벤토리(미장착) 무기의 파츠 상태를 요청 —
    // 호스트가 보관 중인 파츠/탄약을 기존 SendGunStateToGuest로 되돌려준다 (요청한 게스트에게만)
    public static void OnG_RequestGunState(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var packet = root.TypeAsG_RequestGunState();
        if (!RoomManager.TryGetGuestIdFromPacket(packet.PlayerId, autoRegister: false, out int guestId))
            return;

        SendGunStateToGuest(guestId, packet.GunUid);
    }
}
