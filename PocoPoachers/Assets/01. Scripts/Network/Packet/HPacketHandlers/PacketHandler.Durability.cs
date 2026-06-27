public static partial class PacketHandlers
{
    public static void OnH_Durability(FlatPacket root)
    {
        var pkt = root.TypeAsH_Durability();
        EquippableItemBase.FindByUid(pkt.ItemUid)?.SetDurability(pkt.Current);
    }
}
