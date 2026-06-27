public static partial class PacketHandlers
{
    public static void OnG_Durability(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var pkt = root.TypeAsG_Durability();
        var (current, max) = WorldEquipmentManager.ApplyChange(pkt.ItemUid, pkt.ItemId, pkt.Amount, 1f);

        PacketBuilder.BroadcastToGuests(new H_DurabilityT
        {
            ItemUid = pkt.ItemUid,
            Current = current,
            Max     = max,
        }, H_Durability.Pack, PacketType.H_Durability);
    }
}
