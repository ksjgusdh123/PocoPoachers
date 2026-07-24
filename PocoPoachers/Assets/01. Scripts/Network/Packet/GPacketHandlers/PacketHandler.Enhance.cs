public static partial class PacketHandlers
{
    // 게스트가 아이템을 강화한 순간 호스트가 uid 기준으로 강화 레벨을 저장한다.
    // 이후 파츠 장착/총 재장착 시 이 레벨이 그대로 적용·재전송돼 강화가 유지된다.
    public static void OnG_EnhanceItem(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var pkt = root.TypeAsG_EnhanceItem();
        WorldEquipmentManager.SetEnhancementLevel(pkt.ItemUid, pkt.Level, pkt.ItemId);
    }
}
