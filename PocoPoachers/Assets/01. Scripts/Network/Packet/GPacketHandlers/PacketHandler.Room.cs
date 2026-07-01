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
    }
}
