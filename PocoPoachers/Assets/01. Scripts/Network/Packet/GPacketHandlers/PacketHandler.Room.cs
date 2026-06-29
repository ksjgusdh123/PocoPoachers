public static partial class PacketHandlers
{
    public static void OnG_Leave(FlatPacket root)
    {
        var pkt = root.TypeAsG_Leave();
        RoomManager.Instance?.RemoveGuest(pkt.PlayerId);
    }

    public static void OnG_ShelterLevel(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var pkt = root.TypeAsG_ShelterLevel();
        int senderId = RoomManager.LastGuestId;

        ShelterManager.GetInstance()?.SetLevel(pkt.Level);

        PacketBuilder.BroadcastToGuests(senderId,
            new H_ShelterLevelT { Level = pkt.Level },
            H_ShelterLevel.Pack, PacketType.H_ShelterLevel);
    }
}
