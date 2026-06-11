using System;

public class PlayerArmorController : ArmorController
{
    public static event Action<int, ItemData> OnArmorChanged;

    // slotIndex는 무기 슬롯(0~1)에 이어서 방어구 UI의 EquipDropHandler에 2번부터 순서대로 부여
    protected override void OnEquipped(int slotIndex, ItemData data)
    {
        OnArmorChanged?.Invoke(slotIndex, data);
        RoomSync.Equip(data.id, slotIndex);
    }

    protected override void OnUnequipped(int slotIndex)
    {
        OnArmorChanged?.Invoke(slotIndex, null);
        RoomSync.Equip(0, slotIndex);
    }
}
