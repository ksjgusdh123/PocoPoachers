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

        if (prevData != null)
        {
            // 타겟 슬롯에 아이템 있음 → 전체 수량 교환
            int fullSourceAmount = dragged.SavedAmountItem;
            if(dragged.InventoryUI.IsBox) manager.InvokeSlotExchange(_slotUI, dragged, prevData, draggedData, prevAmount, fullSourceAmount);
            else manager.InvokeSlotExchange(dragged, _slotUI, draggedData, prevData, fullSourceAmount, prevAmount);
        }
        else
        {
            //// 타겟 슬롯이 비어 있음 → 드래그한 수량만 이동
            //_slotUI.SetSlotData(draggedData, dragAmount);
            //if (remaining > 0)
            //    dragged.SetSlotData(draggedData, remaining);
            //else
            //    dragged.SetSlotData(null, 0);
            //manager.InvokeSlotDrop(dragged, _slotUI, draggedData, null, dragAmount, 0);
        }
        return true;
    }
}
