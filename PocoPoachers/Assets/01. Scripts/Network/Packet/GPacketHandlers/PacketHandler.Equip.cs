public static partial class PacketHandlers
{
    public static void OnG_Equip(FlatPacket root)
    {
        var pkt = root.TypeAsG_Equip();

        int playerId  = pkt.PlayerId;
        int itemId    = pkt.ItemId;
        int itemUid   = pkt.ItemUid;
        int slotIndex = pkt.SlotIndex;

        if (!ObjectManager.Instance.TryGet(ObjectKind.Player, playerId, out var worldObj)) return;

        var spawned = ApplyRemoteEquip(worldObj, itemId, itemUid, slotIndex);

        if (RoomManager.IsHost)
        {
            PacketBuilder.BroadcastToGuests(playerId,
                new H_EquipT
                {
                    PlayerId  = playerId,
                    ItemId    = itemId,
                    ItemUid   = itemUid,
                    SlotIndex = slotIndex,
                },
                H_Equip.Pack, PacketType.H_Equip);

            // 호스트가 기존 내구도를 조회/등록하고, 장착한 본인(sender 포함) 모두에게 동기화
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
