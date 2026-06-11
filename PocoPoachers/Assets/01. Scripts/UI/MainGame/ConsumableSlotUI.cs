using UnityEngine;

public class ConsumableSlotUI : ItemSlotUIBase
{
    [SerializeField] private int _slotIndex;

    private void Awake()
    {
        QuickSlotDropHandler.OnQuickSlotChanged += OnQuickSlotChanged;
    }

    private void OnQuickSlotChanged(int slotIndex, ItemData data, int amount)
    {
        if (slotIndex != _slotIndex) return;

        SetDisplay(data, amount);
    }
}
