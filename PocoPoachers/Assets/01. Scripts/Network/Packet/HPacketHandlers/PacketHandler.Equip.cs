public static partial class PacketHandlers
{
    public static void OnH_Equip(FlatPacket root)
    {
        var pkt = root.TypeAsH_Equip();
        if (!ObjectManager.Instance.TryGet(ObjectKind.Player, pkt.PlayerId, out var worldObj)) return;

        var mount = worldObj.GetComponent<WeaponMount>();
        if (mount == null) return;

        if (pkt.ItemId == 0)
            mount.ApplyUnequip(pkt.SlotIndex);
        else
            mount.ApplyEquip(pkt.ItemId, pkt.SlotIndex);
    }
}
