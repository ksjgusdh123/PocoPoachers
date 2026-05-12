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

    // 플레이어 ↔ 플레이어: 로컬 교환만
    public void InvokeLocalSwap(ItemSlotUI slotA, ItemSlotUI slotB)
    {
        slotA.InventoryUI.Inventory.SwapSlots(slotA.SlotIndex, slotB.SlotIndex);
    }

    // 플레이어 ↔ 박스: 로컬 + 네트워크 패킷
    public void InvokeNetworkExchange(ItemSlotUI player, ItemSlotUI box, ItemData PtoBItem, ItemData BtoPItem,
        int PtoBMovedAmount, int BtoPMovedAmount, int PSlotIndex, int BSlotIndex)
    {
        player.InventoryUI.OnSlotDropped();

        int boxUid = box.InventoryUI.Box.Id;

        if (!RoomManager.IsHost)
        {
            PacketBuilder.SendToHost(new G_ItemExchangeT
            {
                BoxUid           = boxUid,
                PlayerItemId     = PtoBItem?.id ?? 0,
                PlayerItemAmount = PtoBMovedAmount,
                PlayerSlotIndex  = PSlotIndex,
                BoxItemId        = BtoPItem?.id ?? 0,
                BoxItemAmount    = BtoPMovedAmount,
                BoxSlotIndex     = BSlotIndex,
            }, G_ItemExchange.Pack, PacketType.G_ItemExchange);
        }
        else
        {
            var om = ObjectManager.Instance;
            if (om != null && om.TryGet(ObjectKind.ItemBox, boxUid, out var boxObj))
            {
                var boxInv = boxObj.GetComponent<Inventory>();
                if (boxInv != null)
                {
                    if (BtoPItem != null && BtoPMovedAmount > 0)
                        boxInv.RemoveItemAtSlot(BSlotIndex, BtoPItem, BtoPMovedAmount);
                    if (PtoBItem != null && PtoBMovedAmount > 0)
                        boxInv.AddItemAtSlot(BSlotIndex, PtoBItem, PtoBMovedAmount);
                }
            }

            var playerInv = player.InventoryUI.Inventory;
            if (playerInv != null)
            {
                if (PtoBItem != null && PtoBMovedAmount > 0)
                    playerInv.RemoveItemAtSlot(PSlotIndex, PtoBItem, PtoBMovedAmount);
                if (BtoPItem != null && BtoPMovedAmount > 0)
                    playerInv.AddItemAtSlot(PSlotIndex, BtoPItem, BtoPMovedAmount);
            }

            if (BtoPItem != null)
                PacketBuilder.BroadcastToGuests(new H_ItemBoxUpdateT
                {
                    BoxUid = boxUid,
                    ItemTypeId = BtoPItem.id,
                    Amount = -BtoPMovedAmount,
                    SlotIndex = BSlotIndex,
                }, H_ItemBoxUpdate.Pack, PacketType.H_ItemBoxUpdate);

            if (PtoBItem != null)
                PacketBuilder.BroadcastToGuests(new H_ItemBoxUpdateT
                {
                    BoxUid     = boxUid,
                    ItemTypeId = PtoBItem.id,
                    Amount     = PtoBMovedAmount,
                    SlotIndex  = BSlotIndex,
                }, H_ItemBoxUpdate.Pack, PacketType.H_ItemBoxUpdate);
        }
    }

    // 박스 ↔ 박스: 로컬 교환 + 박스 업데이트 브로드캐스트
    public void InvokeBoxSwap(ItemSlotUI slotA, ItemSlotUI slotB)
    {
        int boxUid = slotA.InventoryUI.Box.Id;
        ItemData dataA = slotA.SlotItemData;
        ItemData dataB = slotB.SlotItemData;
        int amountA = slotA.SavedAmountItem;
        int amountB = slotB.SavedAmountItem;

        slotA.InventoryUI.Inventory.SwapSlots(slotA.SlotIndex, slotB.SlotIndex);

        if (dataA != null)
            PacketBuilder.BroadcastToGuests(new H_ItemBoxUpdateT
            {
                BoxUid     = boxUid,
                ItemTypeId = dataA.id,
                Amount     = amountA,
                SlotIndex  = slotB.SlotIndex,
            }, H_ItemBoxUpdate.Pack, PacketType.H_ItemBoxUpdate);

        if (dataB != null)
            PacketBuilder.BroadcastToGuests(new H_ItemBoxUpdateT
            {
                BoxUid     = boxUid,
                ItemTypeId = dataB.id,
                Amount     = amountB,
                SlotIndex  = slotA.SlotIndex,
            }, H_ItemBoxUpdate.Pack, PacketType.H_ItemBoxUpdate);
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
