using UnityEngine;

public class ArmorController : EquipableController
{
    protected ArmorMount _mount;
    protected StatBase _stat;

    protected virtual void Awake()
    {
        _mount = GetComponent<ArmorMount>();
        _stat = GetComponent<StatBase>();
    }

    public override void Equip(ItemData data, int slotIndex)
    {
        ArmorBase current = _mount.GetArmor();
        if (current != null)
            _stat.RemoveArmorStat(current.Stat);

        ArmorBase armor = _mount.ApplyEquip(data.id);
        if (armor == null) return;

        _stat.ApplyArmorStat(armor.Stat);
        OnEquipped(slotIndex, data);
    }

    public override void Unequip(int slotIndex)
    {
        ArmorBase current = _mount.GetArmor();
        if (current == null) return;

        _stat.RemoveArmorStat(current.Stat);
        _mount.ApplyUnequip();
        OnUnequipped(slotIndex);
    }

    protected virtual void OnEquipped(int slotIndex, ItemData data) { }
    protected virtual void OnUnequipped(int slotIndex) { }
}
