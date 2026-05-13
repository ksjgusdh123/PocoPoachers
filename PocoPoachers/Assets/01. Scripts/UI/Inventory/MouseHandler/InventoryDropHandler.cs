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
        bool draggedSlotIsBox = dragged.InventoryUI.IsBox;

        ItemData draggedData = dragged.SlotItemData;
        int dragAmount = manager.DragAmount;
        int remaining = dragged.SavedAmountItem - dragAmount;
        int draggedSlotIndex = dragged.SlotIndex;

        ItemData prevData = _slotUI.IsSettedItem ? _slotUI.SlotItemData : null;
        int prevAmount = _slotUI.IsSettedItem ? _slotUI.SavedAmountItem : 0;
        int prevSlotIndex = _slotUI.SlotIndex;

        if (prevData != null)
        {
            // 타겟 슬롯에 아이템 있음 → 케이스별 교환
            int fullSourceAmount = dragged.SavedAmountItem;
            bool targetIsBox = _slotUI.InventoryUI.IsBox;

            if (!draggedSlotIsBox && !targetIsBox)
                manager.InvokeLocalSwap(dragged, _slotUI);
            else if (draggedSlotIsBox && targetIsBox)
                manager.InvokeBoxSwap(dragged, _slotUI);
            else if (draggedSlotIsBox)
                manager.InvokeNetworkExchange(_slotUI, dragged, prevData, draggedData, prevAmount, fullSourceAmount, prevSlotIndex, draggedSlotIndex);
            else
                manager.InvokeNetworkExchange(dragged, _slotUI, draggedData, prevData, fullSourceAmount, prevAmount, draggedSlotIndex, prevSlotIndex);
        }
        else
        {
            // 타겟 슬롯이 비어 있음 → 케이스별 이동
            bool targetIsBox = _slotUI.InventoryUI.IsBox;

            if (!draggedSlotIsBox && !targetIsBox)
                manager.InvokeLocalMove(dragged, _slotUI, draggedData, dragAmount);
            else if (draggedSlotIsBox && targetIsBox)
                manager.InvokeBoxMove(dragged, _slotUI, draggedData, dragAmount);
            else
                manager.InvokeNetworkMove(dragged, _slotUI, draggedData, dragAmount);
        }
        return true;
    }
}
