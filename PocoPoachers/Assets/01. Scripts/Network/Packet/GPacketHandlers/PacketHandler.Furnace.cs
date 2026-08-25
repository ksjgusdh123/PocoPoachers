public static partial class PacketHandlers
{
    // 게스트가 화로에 광석을 넣겠다고 요청. 게스트 개인 인벤은 호스트가 추적하지 않으므로
    // 어떤 광석을 얼마나 넣었는지는 G_DropItem과 같은 기준으로 요청 내용을 신뢰하고,
    // 발신자가 방에 등록된 게스트인지와 수량 상한만 검증한다.
    // 화로가 못 받는 경우(다른 광석이 들어있음/가득 참)엔 그대로 환불해야 한다 —
    // 게스트는 이미 자기 인벤에서 뺀 상태라 그냥 무시하면 아이템이 사라진다.
    public static void OnG_FurnaceInsert(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId)) return;
        if (Furnace.Instance == null) return;

        var packet = root.TypeAsG_FurnaceInsert();
        if (packet.Amount <= 0 || packet.Amount > Furnace.MaxInsertAmount) return;

        Furnace.Instance.HandleGuestInsert(guestId, packet.ItemId, packet.Amount);
    }

    // 결과물 수령 / 안 녹은 광석 회수. 무엇을 얼마나 줄지는 호스트의 화로 상태로만 정한다.
    public static void OnG_FurnaceTake(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId)) return;
        if (Furnace.Instance == null) return;

        var packet = root.TypeAsG_FurnaceTake();
        Furnace.Instance.HandleGuestTake(guestId, packet.TakeOutput);
    }
}
