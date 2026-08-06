public static partial class PacketHandlers
{
    // 호스트가 확정한 퀘스트 수락을 그대로 반영한다. 자기가 요청한 경우엔 이미 낙관적으로 적용돼 있어
    // QuestManager.Accept가 조용히 무시한다(이미 InProgress면 재적용 안 함).
    public static void OnH_QuestAccept(FlatPacket root)
    {
        var packet = root.TypeAsH_QuestAccept();
        QuestManager.Accept(packet.QuestId);
    }

    // 호스트가 확정한 퀘스트 완료를 그대로 반영 - Accept와 동일하게 멱등이라 낙관적 적용과 안전하게 합쳐진다.
    // 보상은 여기서 지급하지 않는다 - "완료하기"를 누른 클라이언트가 QuestDescriptionUI.OnClickAction에서
    // 이미 자기 로컬 인벤토리에 지급했다. 여기서 또 주면 전원이 다 받게 되므로 상태 동기화만 한다.
    public static void OnH_QuestComplete(FlatPacket root)
    {
        var packet = root.TypeAsH_QuestComplete();
        QuestManager.Complete(packet.QuestId);
    }

    // 호스트가 확정한 제출량 증가를 반영한다. 제출 요청 시 로컬에는 아직 반영 안 돼 있으므로
    // (QuestDescriptionUI가 게스트일 땐 AddSubmitted를 안 부름) 여기서 처음 실제로 카운트가 오른다.
    public static void OnH_QuestSubmit(FlatPacket root)
    {
        var packet = root.TypeAsH_QuestSubmit();
        QuestManager.AddSubmitted(packet.QuestId, packet.ItemId, packet.Amount);
    }
}
