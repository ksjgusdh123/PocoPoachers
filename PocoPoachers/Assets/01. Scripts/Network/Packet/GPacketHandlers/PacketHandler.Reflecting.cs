public static partial class PacketHandlers
{
    // 게스트가 반사 스킬로 반사 상태가 되었다. 호스트가 적 공격을 판정하므로
    // 이걸 반영하지 않으면 게스트는 반사 스킬을 써도 총알이 (무적 처리로) 그냥 관통만 된다.
    public static void OnG_Reflecting(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId)) return;

        var packet = root.TypeAsG_Reflecting();

        if (ObjectManager.Instance != null &&
            ObjectManager.Instance.TryGet(ObjectKind.Player, guestId, out var playerObj) &&
            playerObj.TryGetComponent<StatBase>(out var stat))
        {
            stat.ApplyReflectingFromNetwork(packet.Value);
        }
    }
}
