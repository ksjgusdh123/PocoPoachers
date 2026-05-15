using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ConsumableSlotUI : MonoBehaviour
{
    [SerializeField] private int _slotIndex;
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _countText;

    private void Awake()
    {
        QuickSlotDropHandler.OnQuickSlotChanged += OnQuickSlotChanged;
    }

    private void OnQuickSlotChanged(int slotIndex, ItemData data, int amount)
    {
        if (slotIndex != _slotIndex) return;

        bool hasItem = data != null;
        _icon.sprite = hasItem ? data.Icon : null;
        _icon.gameObject.SetActive(hasItem);
        _nameText.text = hasItem ? data.ItemName : "";
        _countText.text = hasItem ? amount.ToString() : "";
    }
}
