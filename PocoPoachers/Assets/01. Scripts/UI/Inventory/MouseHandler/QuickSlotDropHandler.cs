using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class QuickSlotDropHandler : EquipDropHandler
{
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private int _quickSlotCount;

    protected override bool OnItemDropped(ItemData data, int amount)
    {
        if(base.OnItemDropped(data, amount))
        {
            _countText.text = amount.ToString();
            return true;
        }
        return false;
    }

    public bool TryRegisterItem()
    {
        SlotInteractionManager manager = SlotInteractionManager.GetInstance();
        ItemSlotUI slotUI = manager.HoveredSlot;
        if (slotUI == null || !slotUI.IsSettedItem) return false;

        ItemData prev = _droppedItemData;
        if (!OnItemDropped(slotUI.SlotItemData, slotUI.SavedAmountItem))
        {
            return false;
        }

        slotUI.EquipItem(prev);
        return true;
    }
}
