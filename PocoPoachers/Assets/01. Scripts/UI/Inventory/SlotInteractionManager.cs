using System;
using UnityEngine;

public class SlotInteractionManager : Singleton<SlotInteractionManager>
{

    // 호버 상태
    public ItemSlotUI HoveredSlot { get; private set; }
    public event Action<ItemSlotUI> OnHoverEnter;
    public event Action<ItemSlotUI> OnHoverExit;

    // 드래그 상태
    public ItemSlotUI DraggedSlot { get; private set; }
    public int DragAmount { get; private set; }
    public CanvasGroup DraggedCanvasGroup { get; private set; }
    public event Action<ItemSlotUI> OnDragBegin;
    public event Action OnDragEnd;

    // Ctrl 클릭 누적 상태
    public ItemSlotUI PendingSlot { get; private set; }
    public int PendingAmount { get; private set; }

    public Inventory InteractionInventory { get; private set; }

    protected override void Awake()
    {
        //FindAnyObjectByType<>
    }

    public void SetInteractionInventory(Inventory inventory)
    {
        InteractionInventory = inventory;
    }

    public void InvokeSlotExchange(ItemSlotUI player, ItemSlotUI box, ItemData PtoBItem, ItemData BtoPItem,
        int PtoBMovedAmount, int BtoPMovedAmount, int PSlotIndex, int BSlotIndex)
    {
        player.InventoryUI.OnSlotDropped();
        PacketBuilder.Send(new C_ExchangeItemReqT
        {
            BoxUid           = box.InventoryUI.Box.Id,
            PlayerItemId     = PtoBItem?.Id ?? 0,
            PlayerItemAmount = PtoBMovedAmount,
            PlayerSlotIndex  = PSlotIndex,
            BoxItemId        = BtoPItem?.Id ?? 0,
            BoxItemAmount    = BtoPMovedAmount,
            BoxSlotIndex     = BSlotIndex,
        }, C_ExchangeItemReq.Pack, PacketType.C_ExchangeItemReq);
    }

    public void InvokeDoubleClick()
    {
        if (HoveredSlot == null || !HoveredSlot.IsSettedItem) return;
        HoveredSlot.InventoryUI?.OnSlotDoubleClicked();
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

    public void SetDragged(ItemSlotUI slot, CanvasGroup canvasGroup, int amount)
    {
        DragAmount = amount;
        DraggedSlot = slot;
        DraggedCanvasGroup = canvasGroup;
        OnDragBegin?.Invoke(slot);
    }

    public void ClearDragged()
    {
        DragIcon.Instance.Hide();
        DraggedSlot = null;
        DraggedCanvasGroup = null;
        DragAmount = 0;
        OnDragEnd?.Invoke();
    }

    public void IncrementPending(ItemSlotUI slot)
    {
        if (PendingSlot != slot)
        {
            PendingSlot = slot;
            PendingAmount = 0;
        }
        if (PendingAmount < slot.SavedAmountItem)
            PendingAmount++;
    }

    public void SetPending(ItemSlotUI slot, int amount)
    {
        PendingSlot = slot;
        PendingAmount = Mathf.Clamp(amount, 1, slot.SavedAmountItem);
    }

    public void ResetPending()
    {
        PendingSlot = null;
        PendingAmount = 0;
    }
}
