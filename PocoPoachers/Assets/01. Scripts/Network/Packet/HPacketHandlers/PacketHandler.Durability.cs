public static partial class PacketHandlers
{
    public static void OnH_Durability(FlatPacket root)
    {
        var packet = root.TypeAsH_Durability();
        EquippableItemBase.FindByUid(packet.ItemUid)?.SetDurability(packet.Current);
    }
}
