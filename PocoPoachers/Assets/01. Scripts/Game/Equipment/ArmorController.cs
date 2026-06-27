using UnityEngine;

public class ArmorController : EquipableController
{
    protected ArmorMount _mount;
    protected StatBase _stat;

    private int _equippedSlotIndex = -1;

    protected virtual void Awake()
    {
        _mount = GetComponent<ArmorMount>();
        _stat = GetComponent<StatBase>();
    }

    public override void Equip(ItemData data, int slotIndex, int uid)
    {
        ArmorBase current = _mount.GetArmor();
        if (current != null)
        {
            _stat.RemoveArmorStat(current.Stat);
            _stat.OnDamaged -= OnDamaged;
        }

        ArmorBase armor = _mount.ApplyEquip(data.id, uid);
        if (armor == null) return;

        _stat.ApplyArmorStat(armor.Stat);
        _stat.OnDamaged += OnDamaged;
        _equippedSlotIndex = slotIndex;
        OnEquipped(slotIndex, data, uid);
    }

    public override void Unequip(int slotIndex)
    {
        ArmorBase current = _mount.GetArmor();
        if (current == null) return;

        _stat.RemoveArmorStat(current.Stat);
        _stat.OnDamaged -= OnDamaged;
        _mount.ApplyUnequip();
        _equippedSlotIndex = -1;
        RaiseUnequipped(slotIndex);
        OnUnequipped(slotIndex);
    }

    private void OnDamaged(float damage, Vector3 _, GameObject __)
    {
        _mount.GetArmor()?.DecreaseDurability(damage);
    }

    public override void UnequipAll()
    {
        if (_mount.GetArmor() != null)
            Unequip(_equippedSlotIndex);
    }

    public override int GetEquippedId(int slotIndex) => _mount.GetEquippedItemId();
    public override int GetEquippedUid(int slotIndex) => _mount.GetArmor()?.Uid ?? 0;

    protected virtual void OnEquipped(int slotIndex, ItemData data, int uid) { }
    protected virtual void OnUnequipped(int slotIndex) { }
}
