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

    // 게스트가 씬 로드를 마친 뒤 보내는 신호. 호스트는 이 시점에 박스/적 스냅샷을 전송한다.
    // (씬 전환 직후 보내면 게스트가 아직 로딩 중이라 유실되므로 게스트 준비 완료를 기다린다)
    public static void OnG_SceneReady(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var packet = root.TypeAsG_SceneReady();
        if (!RoomManager.TryGetGuestIdFromPacket(packet.PlayerId, autoRegister: true, out int guestId))
            return;

        RoomManager.Instance?.HandleGuestSceneReady(guestId);
    }

    // 게스트가 씬 전환 직전에 올린 자기 상태. 호스트는 방 세계 세이브에 기록해 맵 이동마다 오토세이브가 되게 한다.
    // 게스트 복원은 이 패킷을 기다리지 않으므로(로컬 캐리오버) 늦게 도착해도 무방하다.
    public static void OnG_GuestSnapshot(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var packet = root.TypeAsG_GuestSnapshot().UnPack();
        if (!RoomManager.TryGetGuestIdFromPacket(packet.PlayerId, autoRegister: false, out int guestId))
            return;

        MainThreadDispatcher.Enqueue(() => RoomManager.Instance?.StoreGuestSnapshot(guestId, packet));
    }
}
