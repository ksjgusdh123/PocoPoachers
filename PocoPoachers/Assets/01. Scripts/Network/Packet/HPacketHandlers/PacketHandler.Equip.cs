public static partial class PacketHandlers
{
    public static void OnH_Equip(FlatPacket root)
    {
        var packet = root.TypeAsH_Equip();

        // 오브젝트가 아직 없어도 상태는 남긴다 — 스폰 시 RemoteEquipState.ApplyTo가 입혀준다
        RemoteEquipState.SetSlot(packet.PlayerId, packet.SlotIndex, packet.ItemId, packet.ItemUid);

        if (!ObjectManager.Instance.TryGet(ObjectKind.Player, packet.PlayerId, out var worldObj)) return;

        ApplyRemoteArmorStats(worldObj, packet.PlayerId, packet.ItemId, packet.SlotIndex, sendToOthers: false);
        ApplyRemoteEquipVisual(worldObj, packet.ItemId, packet.ItemUid, packet.SlotIndex);
    }

    public static EquippableItemBase ApplyRemoteEquipVisual(WorldObject worldObj, int itemId, int itemUid, int slotIndex)
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

    public static void ApplyRemoteArmorStats(WorldObject worldObj, int playerId, int newItemId, int slotIndex, bool sendToOthers)
    {
        if (slotIndex < 2 || slotIndex == 4) return;
        if (worldObj.GetComponent<RemotePlayerStat>() is not RemotePlayerStat remote) return;

        var mount = worldObj.GetComponent<ArmorMount>();
        int currentId = mount?.GetEquippedItemId() ?? 0;

        if (currentId != 0 && currentId != newItemId)
        {
            var oldData = DataManager.GetArmorStat(currentId);
            if (oldData != null) remote.RemoveArmorStat(oldData);
        }

        if (newItemId != 0 && newItemId != currentId)
        {
            var newData = DataManager.GetArmorStat(newItemId);
            if (newData != null) remote.ApplyArmorStat(newData);
        }

        if (!sendToOthers || !RoomManager.IsHost) return;

        PacketBuilder.BroadcastToGuests(new H_StatSyncT
        {
            PlayerId = playerId,
            Hp       = remote.CurrentHp,
            MaxHp    = remote.MaxHp,
            Stamina  = remote.CurrentStamina,
            Battery  = remote.CurrentBattery,
            Defense  = remote.ArmorDefenseRate,
        }, H_StatSync.Pack, PacketType.H_StatSync);
    }
}
