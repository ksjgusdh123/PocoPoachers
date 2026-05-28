using System;
using UnityEngine;

public class ArmorController : EquipableController
{
    public static event Action<int, ItemData> OnArmorChanged;

    private ArmorBase[] _armors;
    private PlayerStat _playerStat;

    private void Awake()
    {
        _armors = new ArmorBase[4];
        _playerStat = GetComponent<PlayerStat>();
    }

    public override void Equip(ItemData data, int slotIndex)
    {
        if (_armors[slotIndex] != null)
        {
            _playerStat.RemoveArmorStat(_armors[slotIndex].ArmorData);
            Destroy(_armors[slotIndex].gameObject);
        }

        ArmorBase armor = ResourceManager.Instance.Spawn<ArmorBase>(data.prefab, transform);
        if (armor == null) return;

        _playerStat.ApplyArmorStat(armor.ArmorData);
        _armors[slotIndex] = armor;
        OnArmorChanged?.Invoke(slotIndex, data);
    }

    public override void Unequip(int slotIndex)
    {
        if (_armors[slotIndex] == null) return;

        _playerStat.RemoveArmorStat(_armors[slotIndex].ArmorData);
        Destroy(_armors[slotIndex].gameObject);
        _armors[slotIndex] = null;
        OnArmorChanged?.Invoke(slotIndex, null);
    }
}
