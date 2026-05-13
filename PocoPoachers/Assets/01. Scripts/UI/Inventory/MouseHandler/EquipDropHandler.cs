using UnityEngine;

public class EquipDropHandler : ItemHolderDropHandler
{
    [SerializeField] private GameObject _itemVisual;
    [SerializeField] private int slotNumber;

    private WeaponController _weaponController;

    protected override void Awake()
    {
        base.Awake();
        _weaponController = FindAnyObjectByType<WeaponController>();
    }

    protected override bool OnItemDropped(ItemData data, int amount)
    {
        if (!base.OnItemDropped(data, amount)) return false;

        _weaponController.EquipWeapon(data.id, slotNumber);

        if (_itemVisual != null)
            _itemVisual.SetActive(true);
        return true;
    }

    public override void Unequip()
    {
        base.Unequip();
        if (_itemVisual != null)
            _itemVisual.SetActive(false);
    }
}
