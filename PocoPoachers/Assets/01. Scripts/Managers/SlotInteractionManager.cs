using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlotInteractionManager : Singleton<SlotInteractionManager>
{
    // 호버 상태
    public ItemSlotUI HoveredSlot { get; private set; }
    public event Action<ItemSlotUI> OnHoverEnter;
    public event Action<ItemSlotUI> OnHoverExit;

    // 드래그 상태
    public ItemSlotUI DraggedSlot { get; private set; }
    public CanvasGroup DraggedCanvasGroup { get; private set; }
    public event Action<ItemSlotUI> OnDragBegin;
    public event Action OnDragEnd;

    public Inventory InteractionInventory { get; private set; }
    public event Action OnDoubleClick;

    protected override void Awake()
    {
        //FindAnyObjectByType<>
    }

    public void SetInteractionInventory(Inventory inventory)
    {
        InteractionInventory = inventory;
    }

    public void InvokeDoubleClick()
    {
        if (!HoveredSlot.IsSettedItem) return;
        OnDoubleClick?.Invoke();
    }

    public void SetHovered(ItemSlotUI slot)
    {
        HoveredSlot = slot;
        OnHoverEnter?.Invoke(slot);
    }

    public void ClearHovered(ItemSlotUI slot)
    {
        HoveredSlot = null;
        OnHoverExit?.Invoke(slot);
    }

    public void SetDragged(ItemSlotUI slot, CanvasGroup canvasGroup)
    {
        DraggedSlot = slot;
        DraggedCanvasGroup = canvasGroup;
        OnDragBegin?.Invoke(slot);
    }

    public void ClearDragged()
    {
        DraggedSlot = null;
        DraggedCanvasGroup = null;
        OnDragEnd?.Invoke();
    }
}
