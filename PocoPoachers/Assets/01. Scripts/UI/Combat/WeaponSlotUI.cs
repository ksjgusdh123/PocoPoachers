using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotUI : MonoBehaviour
{
    [SerializeField] private int _slotIndex;
    [SerializeField] private Image _icon;

    private void OnEnable()
    {
        WeaponController.OnWeaponChanged += OnWeaponChanged;
    }

    private void OnDisable()
    {
        WeaponController.OnWeaponChanged -= OnWeaponChanged;
    }

    private void OnWeaponChanged(int slotIndex, ItemData data)
    {
        if (slotIndex != _slotIndex - 1) return;

        _icon.sprite = data?.Icon;
        _icon.gameObject.SetActive(data != null);
    }
}
