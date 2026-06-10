using TMPro;
using UnityEngine;

public class ConsumableSlotUI : ItemIconSlotUI
{
    [SerializeField] private int _slotIndex;
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
        SetIcon(data);
        _nameText.text = hasItem ? data.ItemName : "";
        _countText.text = hasItem ? amount.ToString() : "";
    }
}
