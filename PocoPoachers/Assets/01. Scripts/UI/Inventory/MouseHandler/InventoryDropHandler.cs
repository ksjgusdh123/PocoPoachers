using UnityEngine;

// ItemSlotUI와 같은 오브젝트에 추가 - 인벤토리 슬롯끼리 드래그&드롭 교환 처리
public class InventoryDropHandler : BaseDropHandler
{
    private ItemSlotUI _slotUI;

    protected override void Awake()
    {
        base.Awake();
        _slotUI = GetComponent<ItemSlotUI>();
    }

    protected override bool HandleDrop(SlotInteractionManager manager)
    {
        var dragged = manager.DraggedSlot;
        if (dragged == _slotUI) return false;

        ItemData draggedData = dragged.SlotItemData;
        int dragAmount = manager.DragAmount;
        int remaining = dragged.SavedAmountItem - dragAmount;

        ItemData prevData = _slotUI.IsSettedItem ? _slotUI.SlotItemData : null;
        int prevAmount = _slotUI.IsSettedItem ? _slotUI.SavedAmountItem : 0;

        if (prevData != null && prevData == draggedData)
        {
            // 같은 아이템 → 스택 합치기
            int canFit = draggedData.MaxStack - prevAmount;
            if (canFit <= 0) return false;

            //int transferred = Mathf.Min(dragAmount, canFit);
            //int backToSource = remaining + (dragAmount - transferred);

            //_slotUI.SetSlotData(draggedData, prevAmount + transferred);
            //dragged.SetSlotData(backToSource > 0 ? draggedData : null, backToSource);
        }
        else
        {
            // 다른 아이템이거나 빈 슬롯 → 교환
            //if (remaining > 0 && prevData != null) return false;

            //_slotUI.SetSlotData(draggedData, dragAmount);
            //dragged.SetSlotData(remaining > 0 ? draggedData : prevData, remaining > 0 ? remaining : prevAmount);
        }
        manager.InvokeSlotDrop(dragged, _slotUI, draggedData, prevData, dragAmount, prevAmount);

        _slotUI.NotifyInventoryChanged();
        dragged.NotifyInventoryChanged();
        return true;
    }
}
