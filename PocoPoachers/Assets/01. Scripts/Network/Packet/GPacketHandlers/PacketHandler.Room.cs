public static partial class PacketHandlers
{
    public static void OnG_Leave(FlatPacket root)
    {
        var packet = root.TypeAsG_Leave();
        if (!RoomManager.TryGetGuestIdFromPacket(packet.PlayerId, autoRegister: false, out int guestId))
            return;

        RoomManager.Instance?.RemoveGuest(guestId);
    }

    // 게스트가 쉘터 업그레이드를 로컬 재료로 먼저 적용한 뒤 요청. 호스트는 다음 레벨과
    // 일치할 때만 승인하고, 결과(승인/거절 모두)를 모든 인원에게 다시 브로드캐스트해
    // 레이스나 조작된 요청으로 어긋난 로컬 레벨을 호스트 값으로 되돌린다.
    public static void OnG_ShelterLevel(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId))
            return;

        var packet = root.TypeAsG_ShelterLevel();
        var shelter = ShelterManager.GetInstance();
        if (shelter == null) return;

        var nextData = shelter.GetNextLevelData();
        if (nextData != null && packet.Level == nextData.ShelterLevel)
            shelter.SetLevel(packet.Level);

        RoomSync.ShelterLevel(shelter.CurrentLevel);
    }
}
