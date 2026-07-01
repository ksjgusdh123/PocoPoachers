public static partial class PacketHandlers
{
    public static void OnG_Equip(FlatPacket root)
    {
        var pkt = root.TypeAsG_Equip();
        if (!RoomManager.TryResolveGuestSender(pkt.PlayerId, allowAutoRegister: false, out int guestId))
            return;

        int itemId    = pkt.ItemId;
        int itemUid   = pkt.ItemUid;
        int slotIndex = pkt.SlotIndex;

        if (!ObjectManager.Instance.TryGet(ObjectKind.Player, guestId, out var worldObj)) return;

        var spawned = ApplyRemoteEquip(worldObj, itemId, itemUid, slotIndex);

        if (RoomManager.IsHost)
        {
            PacketBuilder.BroadcastToGuests(guestId,
                new H_EquipT
                {
                    PlayerId  = guestId,
                    ItemId    = itemId,
                    ItemUid   = itemUid,
                    SlotIndex = slotIndex,
                },
                H_Equip.Pack, PacketType.H_Equip);

            if (spawned != null && itemUid != 0)
            {
                var (current, max) = WorldEquipmentManager.GetOrCreate(itemUid, itemId, spawned.MaxDurability);
                spawned.SetDurability(current);
                PacketBuilder.BroadcastToGuests(new H_DurabilityT { ItemUid = itemUid, Current = current, Max = max },
                    H_Durability.Pack, PacketType.H_Durability);
            }
        }
    }
}
