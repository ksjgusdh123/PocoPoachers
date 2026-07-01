public static partial class PacketHandlers
{
    public static void OnH_Equip(FlatPacket root)
    {
        var pkt = root.TypeAsH_Equip();
        if (!ObjectManager.Instance.TryGet(ObjectKind.Player, pkt.PlayerId, out var worldObj)) return;

        ApplyRemoteEquip(worldObj, pkt.ItemId, pkt.ItemUid, pkt.SlotIndex);
        SyncRemoteArmorDefense(worldObj, pkt.PlayerId, pkt.ItemId, pkt.SlotIndex, broadcast: false);
    }

    static EquippableItemBase ApplyRemoteEquip(WorldObject worldObj, int itemId, int itemUid, int slotIndex)
    {
        if (slotIndex == 4)
        {
            var bagMount = worldObj.GetComponent<BagMount>();
            if (bagMount == null) return null;

            if (itemId == 0)
                bagMount.ApplyUnequip();
            else
                bagMount.ApplyEquip(itemId, itemUid);
            return null;
        }

        if (slotIndex >= 2)
        {
            var armorMount = worldObj.GetComponent<ArmorMount>();
            if (armorMount == null) return null;

            if (itemId == 0)
            {
                armorMount.ApplyUnequip();
                return null;
            }
            return armorMount.ApplyEquip(itemId, itemUid);
        }

        var mount = worldObj.GetComponent<WeaponMount>();
        if (mount == null) return null;

        if (itemId == 0)
        {
            mount.ApplyUnequip(slotIndex);
            return null;
        }
        return mount.ApplyEquip(itemId, slotIndex, itemUid);
    }

    static void SyncRemoteArmorDefense(WorldObject worldObj, int playerId, int itemId, int slotIndex, bool broadcast)
    {
        if (slotIndex < 2) return;
        if (worldObj.GetComponent<RemotePlayerStat>() is not RemotePlayerStat remote) return;

        float defense = 0f;
        if (itemId != 0)
        {
            var armorStat = DataManager.GetArmorStat(itemId);
            if (armorStat != null) defense = armorStat.DefenseRate;
        }

        remote.SetArmorDefenseRate(defense);

        if (!broadcast || !RoomManager.IsHost) return;

        PacketBuilder.BroadcastToGuests(new H_StatSyncT
        {
            PlayerId = playerId,
            Hp       = remote.CurrentHp,
            MaxHp    = remote.MaxHp,
            Stamina  = remote.Stamina,
            Battery  = remote.Battery,
            Defense  = defense,
        }, H_StatSync.Pack, PacketType.H_StatSync);
    }
}
