public static partial class PacketHandlers
{
    public static void OnG_GunPartEquip(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var pkt = root.TypeAsG_GunPartEquip();
        var slotType = (SlotType)pkt.SlotType;

        // 호스트에 존재하는 해당 uid의 총 인스턴스(원격 플레이어 포함)도 함께 갱신
        if (EquippableItemBase.FindByUid(pkt.GunUid) is GunBase gun)
        {
            if (pkt.PartId != 0)
            {
                var part = GunPartTable.Instance.Get(pkt.PartId);
                if (part != null) gun.EquipPart(part);
            }
            else
            {
                gun.UnequipPart(slotType);
            }
            gun.SetAmmo(pkt.CurrentAmmo);
        }

        if (pkt.PartId != 0)
            WorldEquipmentManager.SetPart(pkt.GunUid, slotType, pkt.PartId);
        else
            WorldEquipmentManager.RemovePart(pkt.GunUid, slotType);
        WorldEquipmentManager.SetAmmo(pkt.GunUid, pkt.CurrentAmmo, pkt.MaxMagazine);
    }
}
