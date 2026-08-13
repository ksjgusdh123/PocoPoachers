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

    // 게스트의 이동 동의 응답. 호스트는 전원 수락이 모이면 실제 씬 전환을 시작한다.
    public static void OnG_MoveReply(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var packet = root.TypeAsG_MoveReply();
        if (!RoomManager.TryGetGuestIdFromPacket(packet.PlayerId, autoRegister: false, out int guestId))
            return;

        bool accepted = packet.Accepted;
        MainThreadDispatcher.Enqueue(() => SceneMoveVote.Instance?.HandleGuestReply(guestId, accepted));
    }

    // 게스트가 보고한 자기 닉네임. 명부에 넣고 갱신된 전체 명부를 전원에게 다시 뿌린다.
    // 패킷 안의 player_id는 믿지 않는다 — 실제 송신자 id로 키를 잡아 남의 이름을 덮어쓰지 못하게 한다.
    public static void OnG_Nickname(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        // 접속 직후라 아직 게스트 등록 전일 수 있어 autoRegister를 켠다 (G_SceneReady와 같은 이유).
        // 자동 등록도 대기 목록의 엔드포인트와 일치할 때만 통과하므로 사칭은 막힌다.
        var packet = root.TypeAsG_Nickname();
        if (!RoomManager.TryGetGuestIdFromPacket(packet.PlayerId, autoRegister: true, out int guestId))
            return;

        string nickname = packet.Nickname;
        MainThreadDispatcher.Enqueue(() =>
        {
            PlayerNameRegistry.Set(guestId, nickname);
            // 방 세계에 남겨 이 게스트가 다음에 들어와도 같은 이름을 쓰게 한다
            SaveManager.GetInstance()?.SaveGuestNickname(guestId, PlayerNameRegistry.Get(guestId));
            RoomSync.Roster();
        });
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
