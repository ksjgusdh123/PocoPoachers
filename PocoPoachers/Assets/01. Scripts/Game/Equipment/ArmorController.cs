using System;
using UnityEngine;

public class ArmorController : EquipableController
{
    public static event Action<int, ItemData> OnArmorChanged;

    private ArmorMount _mount;
    private PlayerStat _playerStat;

    private void Awake()
    {
        _mount = GetComponent<ArmorMount>();
        _playerStat = GetComponent<PlayerStat>();
    }

    public override void Equip(ItemData data, int slotIndex)
    {
        ArmorBase current = _mount.GetArmor();
        if (current != null)
            _playerStat.RemoveArmorStat(current.ArmorData);

        ArmorBase armor = _mount.ApplyEquip(data.id);
        if (armor == null) return;

        _playerStat.ApplyArmorStat(armor.ArmorData);
        OnArmorChanged?.Invoke(slotIndex, data);
    }

    public override void Unequip(int slotIndex)
    {
        ArmorBase current = _mount.GetArmor();
        if (current == null) return;

        _playerStat.RemoveArmorStat(current.ArmorData);
        _mount.ApplyUnequip();
        OnArmorChanged?.Invoke(slotIndex, null);
    }
}
