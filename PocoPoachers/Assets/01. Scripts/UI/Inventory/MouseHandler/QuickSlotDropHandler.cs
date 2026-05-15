using System;
using TMPro;
using UnityEngine;

public class QuickSlotDropHandler : ItemHolderDropHandler
{
    public static event Action<int, ItemData, int> OnQuickSlotChanged;

    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private int _quickSlotCount;


    protected override bool OnItemDropped(ItemData data, int amount)
    {
        if (!base.OnItemDropped(data, amount)) return false;

        _countText.text = amount.ToString();
        OnQuickSlotChanged?.Invoke(_quickSlotCount, data, amount);
        return true;
    }

    public bool TryRegisterItem()
    {
        SlotInteractionManager manager = SlotInteractionManager.GetInstance();
        ItemSlotUI slotUI = manager.HoveredSlot;
        if (slotUI == null || !slotUI.IsSettedItem) return false;

        ItemData prev = DroppedItemData;
        int prevAmount = DroppedAmount;
        if (!OnItemDropped(slotUI.SlotItemData, slotUI.SavedAmountItem))
            return false;

        if (prev == null)
            slotUI.ClearSlot();
        else
            slotUI.EquipItem(prev, prevAmount);
        return true;
    }

    public void ConsumeItem()
    {
        if (--DroppedAmount <= 0)
        {
            Unequip();
        }
        else
        {
            _countText.text = DroppedAmount.ToString();
            OnQuickSlotChanged?.Invoke(_quickSlotCount, DroppedItemData, DroppedAmount);
        }
    }

    public override void Unequip()
    {
        OnQuickSlotChanged?.Invoke(_quickSlotCount, null, 0);
        base.Unequip();
        _countText.text = "";
    }
}
