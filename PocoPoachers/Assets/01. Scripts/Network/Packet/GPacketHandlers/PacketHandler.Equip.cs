public static partial class PacketHandlers
{
    public static void OnG_Equip(FlatPacket root)
    {
        var packet = root.TypeAsG_Equip();
        if (!RoomManager.TryGetGuestIdFromPacket(packet.PlayerId, autoRegister: false, out int guestId))
            return;

        int itemId    = packet.ItemId;
        int itemUid   = packet.ItemUid;
        int slotIndex = packet.SlotIndex;

        // 오브젝트가 아직 없어도 상태는 남긴다 — 스폰 시 RemoteEquipState.ApplyTo가 입혀준다
        RemoteEquipState.SetSlot(guestId, slotIndex, itemId, itemUid);

        EquippableItemBase spawned = null;
        if (ObjectManager.Instance.TryGet(ObjectKind.Player, guestId, out var worldObj))
        {
            ApplyRemoteArmorStats(worldObj, guestId, itemId, slotIndex, sendToOthers: true);
            spawned = ApplyRemoteEquipVisual(worldObj, itemId, itemUid, slotIndex);
        }

        if (RoomManager.IsHost)
        {
            PacketBuilder.BroadcastReliableToGuests(guestId,
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
