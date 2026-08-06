public static partial class PacketHandlers
{
    // 게스트의 퀘스트 수락 요청. 호스트가 권위적으로 적용하고, 확인 겸 전원(요청한 게스트 포함)에게
    // 다시 브로드캐스트해 다른 플레이어들 화면도 같이 갱신한다 - OnG_ShelterLevel과 동일한 패턴.
    public static void OnG_QuestAccept(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var packet = root.TypeAsG_QuestAccept();
        QuestManager.Accept(packet.QuestId);
        RoomSync.QuestAccept(packet.QuestId);
    }

    // 게스트의 퀘스트 완료 요청 - Accept와 동일한 패턴(상태 확정형이라 멱등). 요청한 게스트가 이미
    // 자기 로컬에서 보상을 지급했으므로(QuestDescriptionUI.OnClickAction) 호스트는 상태만 맞추고
    // 절대 보상을 지급하지 않는다 - "완료버튼 누른 사람만" 받는 정책.
    public static void OnG_QuestComplete(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var packet = root.TypeAsG_QuestComplete();
        QuestManager.Complete(packet.QuestId);
        RoomSync.QuestComplete(packet.QuestId);
    }

    // 게스트의 퀘스트 제출 요청. 게스트는 아이템만 자기 인벤토리에서 미리 빼고 제출량은 로컬에
    // 반영하지 않은 채 요청만 보낸다 - 호스트가 여기서 누적치를 직접 올리고, 그 결과를 전원에게
    // 브로드캐스트해야 요청한 게스트를 포함해 모두 같은 값으로 수렴한다(이중 집계 방지).
    public static void OnG_QuestSubmit(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var packet = root.TypeAsG_QuestSubmit();
        QuestManager.AddSubmitted(packet.QuestId, packet.ItemId, packet.Amount);
        RoomSync.QuestSubmit(packet.QuestId, packet.ItemId, packet.Amount);
    }
}
