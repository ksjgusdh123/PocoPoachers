public static partial class PacketHandlers
{
    public static void OnG_Leave(FlatPacket root)
    {
        var pkt = root.TypeAsG_Leave();
        RoomManager.Instance?.RemoveGuest(pkt.PlayerId);
    }
}
