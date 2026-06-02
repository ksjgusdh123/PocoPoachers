using System;

public class PlayerArmorController : ArmorController
{
    public static event Action<int, ItemData> OnArmorChanged;

    protected override void OnEquipped(int slotIndex, ItemData data) =>
        OnArmorChanged?.Invoke(slotIndex, data);

    protected override void OnUnequipped(int slotIndex) =>
        OnArmorChanged?.Invoke(slotIndex, null);
}
