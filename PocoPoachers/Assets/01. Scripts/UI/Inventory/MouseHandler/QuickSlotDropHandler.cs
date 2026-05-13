using TMPro;
using UnityEngine;

public class QuickSlotDropHandler : ItemHolderDropHandler
{
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private int _quickSlotCount;


    protected override bool OnItemDropped(ItemData data, int amount)
    {
        if (!base.OnItemDropped(data, amount)) return false;

        _countText.text = amount.ToString();
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
        }
    }

    public override void Unequip()
    {
        base.Unequip();
        _countText.text = "";
    }
}
