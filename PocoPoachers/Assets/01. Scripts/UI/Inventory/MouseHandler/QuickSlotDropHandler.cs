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
        int prevAmount = _droppedAmount;
        if (!OnItemDropped(slotUI.SlotItemData, slotUI.SavedAmountItem))
            return false;

        if (prev == null)
            slotUI.ClearSlot();  // 퀵슬롯이 비어 있었으면 인벤토리 슬롯 비우기
        else
            slotUI.EquipItem(prev, prevAmount);  // 이전 퀵슬롯 아이템과 교환
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
}
