public static partial class PacketHandlers
{
    public static void OnG_Leave(FlatPacket root)
    {
        var pkt = root.TypeAsG_Leave();
        if (!RoomManager.TryResolveGuestSender(pkt.PlayerId, allowAutoRegister: false, out int guestId))
            return;

        RoomManager.Instance?.RemoveGuest(guestId);
    }

    public static void OnG_ShelterLevel(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;
        if (!RoomManager.TryResolveGuestSender(0, allowAutoRegister: false, out int senderId))
            return;

        var pkt = root.TypeAsG_ShelterLevel();

        ShelterManager.GetInstance()?.SetLevel(pkt.Level);

        PacketBuilder.BroadcastToGuests(senderId,
            new H_ShelterLevelT { Level = pkt.Level },
            H_ShelterLevel.Pack, PacketType.H_ShelterLevel);
    }
}
