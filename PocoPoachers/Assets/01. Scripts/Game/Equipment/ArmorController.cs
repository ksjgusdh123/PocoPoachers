using System.Collections.Generic;

public class ArmorController : EquipableController
{
    private readonly Dictionary<int, ItemData> _equippedArmors = new Dictionary<int, ItemData>();

    public override void Equip(ItemData data, int slotIndex)
    {
        _equippedArmors[slotIndex] = data;
    }

    public override void Unequip(int slotIndex)
    {
        _equippedArmors.Remove(slotIndex);
    }
}
