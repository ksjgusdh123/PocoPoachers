using TMPro;
using UnityEngine;

public class WeaponSlotUI : SlotUIBase
{
    [SerializeField] TextMeshProUGUI _nameText;
    [SerializeField] private int _slotIndex;

    private void Awake()
    {
        WeaponController.OnWeaponChanged += OnWeaponChanged;
    }

    private void OnDestroy()
    {
        WeaponController.OnWeaponChanged -= OnWeaponChanged;
    }

    private void OnWeaponChanged(int slotIndex, ItemData data)
    {
        if (slotIndex != _slotIndex - 1) return;

        _nameText.text = LocalizationManager.GetInstance().GetString(data.ItemName);
        SetIcon(data);
    }
}
