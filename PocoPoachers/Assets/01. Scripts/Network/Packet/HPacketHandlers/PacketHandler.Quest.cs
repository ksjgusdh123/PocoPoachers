public static partial class PacketHandlers
{
    // 호스트가 확정한 절대값만 반영한다. 변경 번호로 늦게 도착한 예전 상태를 거른다.
    public static void OnH_QuestAccept(FlatPacket root)
    {
        if (RoomManager.IsHost) return;
        QuestManager.ApplySnapshot(root.TypeAsH_QuestAccept().UnPack());
    }
    public static void OnH_QuestComplete(FlatPacket root)
    {
        if (RoomManager.IsHost) return;
        var packet = root.TypeAsH_QuestComplete();
        if (packet.PlayerId == QuestManager.LocalPlayerId) QuestManager.ReceiveReward(packet.QuestId);
    }
    public static void OnH_QuestSubmit(FlatPacket root)
    {
        if (RoomManager.IsHost) return;
        QuestManager.ApplySubmissionResult(root.TypeAsH_QuestSubmit().UnPack());
    }
}
