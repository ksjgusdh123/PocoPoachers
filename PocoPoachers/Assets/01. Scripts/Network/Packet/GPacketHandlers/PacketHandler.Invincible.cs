public static partial class PacketHandlers
{
    // 게스트가 구르기 등으로 무적이 되었다. 호스트가 적 공격을 판정하므로
    // 이걸 반영하지 않으면 게스트는 굴러도 그대로 맞는다.
    public static void OnG_Invincible(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryGetGuestIdFromPacket(0, autoRegister: false, out int guestId)) return;

        var packet = root.TypeAsG_Invincible();

        if (ObjectManager.Instance != null &&
            ObjectManager.Instance.TryGet(ObjectKind.Player, guestId, out var playerObj) &&
            playerObj.TryGetComponent<StatBase>(out var stat))
        {
            stat.ApplyImmunityFromNetwork(packet.Value);
        }
    }
}
