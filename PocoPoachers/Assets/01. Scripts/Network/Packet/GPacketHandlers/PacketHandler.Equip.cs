public static partial class PacketHandlers
{
    public static void OnG_Equip(FlatPacket root)
    {
        var pkt = root.TypeAsG_Equip();

        int playerId  = pkt.PlayerId;
        int itemId    = pkt.ItemId;
        int slotIndex = pkt.SlotIndex;

        if (!ObjectManager.Instance.TryGet(ObjectKind.Player, playerId, out var worldObj)) return;
        var mount = worldObj.GetComponent<WeaponMount>();
        if (mount == null) return;

        if (itemId == 0)
            mount.ApplyUnequip(slotIndex);
        else
            mount.ApplyEquip(itemId, slotIndex);

        if (RoomManager.IsHost)
        {
            PacketBuilder.BroadcastToGuests(playerId,
                new H_EquipT
                {
                    PlayerId  = playerId,
                    ItemId    = itemId,
                    SlotIndex = slotIndex,
                },
                H_Equip.Pack, PacketType.H_Equip);
        }
    }
}
