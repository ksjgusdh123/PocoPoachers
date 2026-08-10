using UnityEngine;

public static partial class PacketHandlers
{
    public static void OnH_GuestJoined(FlatPacket root)
    {
        var packet = root.TypeAsH_GuestJoined();

        var rm = RoomManager.Instance;
        if (rm != null)
        {
            if (packet.InfoLength == 1)
                rm.AddMember();
            else if (RoomManager.MemberCount <= 1)
                rm.SetMemberCount(packet.InfoLength + 1);
        }

        for (int i = 0; i < packet.InfoLength; i++)
        {
            if (!packet.Info(i).HasValue) continue;
            var info = packet.Info(i).Value;
            var p = info.Pos;

            // 위치 없는 합류 알림은 멤버 수만 갱신. 스폰은 이후 H_Move에서 처리한다.
            if (packet.InfoLength == 1 && p.X == 0f && p.Y == 0f && p.Z == 0f)
                continue;

            ObjectManager.Instance?.QueueMove(
                ObjectKind.Player, info.PlayerId,
                new Vector3(p.X, p.Y, p.Z), info.Rotation, 0);
        }
    }

    public static void OnH_Leave(FlatPacket root)
    {
        var packet = root.TypeAsH_Leave();
        if (packet.IsHost)
            RoomManager.Instance?.HandleHostLeft();
        else
        {
            ObjectManager.Instance?.Despawn(ObjectKind.Player, packet.PlayerId);
            RemoteEquipState.ClearPlayer(packet.PlayerId);
            RoomManager.Instance?.RemoveMember();
        }
    }

    public static void OnH_ShelterLevel(FlatPacket root)
    {
        var packet = root.TypeAsH_ShelterLevel();
        ShelterManager.GetInstance()?.SetLevel(packet.Level);
    }

    // 접속 시 호스트가 보낸 방 세계 상태를 로컬 플레이어에 복원한다.
    public static void OnH_GuestRestore(FlatPacket root)
    {
        var packet = root.TypeAsH_GuestRestore().UnPack();
        MainThreadDispatcher.Enqueue(() =>
        {
            var player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
            if (player == null) return;
            player.ApplyRoomRestore(packet.Equips, packet.Inventory, packet.QuickSlots);
        });
    }

    // 호스트의 팀 이동 제안. 게스트는 수락/거절 팝업을 띄우고 G_MoveReply로 답한다.
    public static void OnH_MoveRequest(FlatPacket root)
    {
        var packet = root.TypeAsH_MoveRequest();
        string sceneName = packet.SceneName;
        if (string.IsNullOrEmpty(sceneName)) return;

        MainThreadDispatcher.Enqueue(() => SceneMoveVote.GetInstance()?.ShowRequest(sceneName));
    }

    // 호스트가 뿌린 투표 현황. 게스트도 같은 인원 아이콘 열을 그린다.
    public static void OnH_MoveProgress(FlatPacket root)
    {
        var packet = root.TypeAsH_MoveProgress().UnPack();
        int[]  memberIds = packet.MemberIds?.ToArray();
        bool[] accepted  = packet.Accepted?.ToArray();

        MainThreadDispatcher.Enqueue(() => SceneMoveVote.GetInstance()?.HandleProgress(memberIds, accepted));
    }

    // 탈출 구역 상태. 게스트는 판정하지 않고 알림 UI와 결과창만 호스트에 맞춘다.
    public static void OnH_EscapeState(FlatPacket root)
    {
        var packet = root.TypeAsH_EscapeState().UnPack();
        bool[] inside    = packet.Inside?.ToArray();
        int[]  memberIds = packet.MemberIds?.ToArray();

        MainThreadDispatcher.Enqueue(() =>
            EscapeZone.ApplyRemoteState(packet.Active, packet.Duration, packet.Completed, inside, packet.Charging, memberIds, packet.ZoneId));
    }

    // 거절·시간 초과·호스트 취소로 이동이 무산됐다는 통보.
    public static void OnH_MoveCancel(FlatPacket root)
    {
        MainThreadDispatcher.Enqueue(() => SceneMoveVote.GetInstance()?.HandleCancelled());
    }

    public static void OnH_LoadScene(FlatPacket root)
    {
        var packet = root.TypeAsH_LoadScene();
        string sceneName = packet.SceneName;
        if (string.IsNullOrEmpty(sceneName)) return;

        MainThreadDispatcher.Enqueue(() =>
        {
            if (packet.SpawnId != 0)
                GameManager.Instance?.SetSpawnId((SpawnId)packet.SpawnId);

            SceneTransition.LoadLocal(sceneName);
        });
    }
}
