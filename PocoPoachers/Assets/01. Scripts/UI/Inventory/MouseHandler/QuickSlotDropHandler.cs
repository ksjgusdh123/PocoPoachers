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

        ItemData prev = _droppedItemData;
        int prevAmount = _droppedAmount;
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
        if (--_droppedAmount <= 0)
        {
            Unequip();
        }
        else
        {
            _countText.text = _droppedAmount.ToString();
        }
    }

    public override void Unequip()
    {
        base.Unequip();
        _countText.text = "";
    }
}
