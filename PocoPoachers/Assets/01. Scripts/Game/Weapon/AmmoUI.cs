using TMPro;
using UnityEngine;

public class AmmoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _currentAmmoText;
    [SerializeField] private TextMeshProUGUI _inventoryAmmoText;
    [SerializeField] private float[] _slotPositionsX = { 60f, 180f };

    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        gameObject.SetActive(false);

        WeaponController.OnAmmoChanged += UpdateAmmoDisplay;
        WeaponController.OnWeaponSwitched += UpdatePosition;
        WeaponController.OnWeaponChanged += OnWeaponChanged;
    }

    private void OnDestroy()
    {
        WeaponController.OnAmmoChanged -= UpdateAmmoDisplay;
        WeaponController.OnWeaponSwitched -= UpdatePosition;
        WeaponController.OnWeaponChanged -= OnWeaponChanged;
    }

    private void UpdateAmmoDisplay(int current, int inventoryCount)
    {
        _currentAmmoText.text = current.ToString();
        _inventoryAmmoText.text = inventoryCount.ToString();
    }

    private void OnWeaponChanged(int slotIndex, ItemData data)
    {
        if (data == null && gameObject.activeSelf)
            gameObject.SetActive(false);
    }

    private void UpdatePosition(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _slotPositionsX.Length) return;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        Vector2 pos = _rectTransform.anchoredPosition;
        pos.x = _slotPositionsX[slotIndex];
        _rectTransform.anchoredPosition = pos;
    }
}
