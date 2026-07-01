public static partial class PacketHandlers
{
    public static void OnG_Leave(FlatPacket root)
    {
        var packet = root.TypeAsG_Leave();
        if (!RoomManager.TryGetGuestIdFromPacket(packet.PlayerId, autoRegister: false, out int guestId))
            return;

        RoomManager.Instance?.RemoveGuest(guestId);
    }

    public static void OnG_ShelterLevel(FlatPacket root)
    {
    }
}
