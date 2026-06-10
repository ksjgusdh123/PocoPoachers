using UnityEngine;

public class WeaponSlotUI : ItemIconSlotUI
{
    [SerializeField] private int _slotIndex;

    private void Awake()
    {
        WeaponController.OnWeaponChanged += OnWeaponChanged;
    }

    private void OnWeaponChanged(int slotIndex, ItemData data)
    {
        if (slotIndex != _slotIndex - 1) return;

        SetIcon(data);
    }
}
