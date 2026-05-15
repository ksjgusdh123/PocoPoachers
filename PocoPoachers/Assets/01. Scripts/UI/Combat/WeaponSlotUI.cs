using UnityEngine;
using UnityEngine.UI;

public class WeaponSlotUI : MonoBehaviour
{
    [SerializeField] private int _slotIndex;
    [SerializeField] private Image _icon;

    private void Awake()
    {
        WeaponController.OnWeaponChanged += OnWeaponChanged;
    }

    private void OnWeaponChanged(int slotIndex, ItemData data)
    {
        if (slotIndex != _slotIndex - 1) return;

        _icon.sprite = data != null ? ResourceManager.Instance.LoadSprite(data.icon) : null;
        _icon.gameObject.SetActive(data != null);
    }
}
