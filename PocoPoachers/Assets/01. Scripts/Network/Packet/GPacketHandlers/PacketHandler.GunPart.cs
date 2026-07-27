public static partial class PacketHandlers
{
    public static void OnG_GunPartEquip(FlatPacket root)
    {
        if (!RoomManager.IsHost) return;

        var pkt = root.TypeAsG_GunPartEquip();
        var slotType = (SlotType)pkt.SlotType;

        // 게스트가 로컬에서 계산한 강화 레벨을 호스트가 partUid 기준으로 저장한다.
        // 이후 GetEnhancedGunPart / SendGunStateToGuest가 이 값을 그대로 사용해 강화가 유지된다.
        if (pkt.PartId != 0 && pkt.PartUid != 0)
            WorldEquipmentManager.SetEnhancementLevel(pkt.PartUid, pkt.PartLevel, pkt.PartId);

        // 호스트에 존재하는 해당 uid의 총 인스턴스(원격 플레이어 포함)도 함께 갱신
        if (EquippableItemBase.FindByUid(pkt.GunUid) is GunBase gun)
        {
            if (pkt.PartId != 0)
            {
                var part = GunPartTable.Instance.Get(pkt.PartId);
                if (part != null) gun.EquipPart(WorldEquipmentManager.GetEnhancedGunPart(part, pkt.PartUid));
            }
            else
            {
                gun.UnequipPart(slotType);
            }
            gun.SetAmmo(pkt.CurrentAmmo);
        }

        if (pkt.PartId != 0)
            WorldEquipmentManager.SetPart(pkt.GunUid, slotType, pkt.PartId, pkt.PartUid);
        else
            WorldEquipmentManager.RemovePart(pkt.GunUid, slotType);
        WorldEquipmentManager.SetAmmo(pkt.GunUid, pkt.CurrentAmmo, pkt.MaxMagazine);
    }
}
