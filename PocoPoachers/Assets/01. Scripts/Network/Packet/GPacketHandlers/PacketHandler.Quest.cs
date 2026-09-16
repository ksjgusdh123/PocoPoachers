public static partial class PacketHandlers
{
    // 소유자 ID는 요청 내용이 아니라 실제 UDP 송신자로부터 결정한다.
    public static void OnG_QuestAccept(FlatPacket root)
    {
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int playerId)) return;
        RoomSync.HostQuestAccept(playerId, root.TypeAsG_QuestAccept().QuestId);
    }
    public static void OnG_QuestComplete(FlatPacket root)
    {
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int playerId)) return;
        RoomSync.HostQuestComplete(playerId, root.TypeAsG_QuestComplete().QuestId);
    }
    public static void OnG_QuestSubmit(FlatPacket root)
    {
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int playerId)) return;
        var packet = root.TypeAsG_QuestSubmit();
        RoomSync.HostQuestSubmit(playerId, packet.QuestId, packet.ItemId, packet.Amount, packet.RequestId);
    }
}
