public static partial class PacketHandlers
{
    public static void OnH_Equip(FlatPacket root)
    {
        var pkt = root.TypeAsH_Equip();
        if (!ObjectManager.Instance.TryGet(ObjectKind.Player, pkt.PlayerId, out var worldObj)) return;

        ApplyRemoteEquip(worldObj, pkt.ItemId, pkt.ItemUid, pkt.SlotIndex);
    }

    // 원격 플레이어의 장착 상태 적용 (UI EquipDropHandler의 _slotIndex 규칙과 동일)
    // 슬롯 0~1: 무기, 2~4: 방어구, 5: 가방
    // 반환값: 새로 스폰된 장비 인스턴스(해제/가방이면 null) — 호스트가 내구도 복원에 사용
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

            // 현재 ArmorMount는 헬멧 단일 슬롯이라 인덱스를 받지 않음
            // 방어구 종류가 늘어나면 slotIndex를 전달하도록 확장
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
}
