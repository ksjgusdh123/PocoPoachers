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

    // 장비 슬롯 우클릭 상태
    public event Action<ItemHolderDropHandler> OnEquipRightClick;

    // 슬롯 간 드래그로 아이템이 배치(스왑/이동/장착) 완료됨
    public event Action OnItemPlaced;

    // 슬롯 간 드래그를 내려놓았지만 배치에 실패함
    public event Action OnItemPlaceFailed;

    // Ctrl 클릭 누적 상태
    public ItemSlotUI PendingSlot { get; private set; }
    public int PendingAmount { get; private set; }

    // 플레이어 ↔ 플레이어: 로컬 교환만
    public void InvokeLocalSwap(ItemSlotUI slotA, ItemSlotUI slotB)
    {
        slotA.InventoryUI.Inventory.SwapSlots(slotA.SlotIndex, slotB.SlotIndex);
    }

    // 플레이어 ↔ 박스: 로컬 + 네트워크 패킷
    public void InvokeNetworkExchange(ItemSlotUI player, ItemSlotUI box)
    {
        player.InventoryUI.OnSlotDropped();

        ItemData playerItem = player.SlotItemData;
        ItemData boxItem    = box.SlotItemData;
        int playerAmount    = player.SavedAmountItem;
        int boxAmount       = box.SavedAmountItem;
        int playerSlot      = player.SlotIndex;
        int boxSlot         = box.SlotIndex;
        int boxUid          = box.InventoryUI.Box.Id;

        ApplyExchangeLocally(player, boxUid, boxSlot, boxItem, boxAmount, playerSlot, playerItem, playerAmount);

        if (!RoomManager.IsHost)
        {
            // 낙관적 업데이트: 즉시 로컬 적용 후 호스트에 요청
            RoomSync.ItemExchange(boxUid, playerItem?.id ?? 0, playerAmount, playerSlot, boxItem?.id ?? 0, boxAmount, boxSlot);
        }
        else
        {
            if (boxItem != null)
                RoomSync.ItemBoxUpdate(boxUid, boxItem.id, -boxAmount, boxSlot);

            if (playerItem != null)
                RoomSync.ItemBoxUpdate(boxUid, playerItem.id, playerAmount, boxSlot);
        }
    }

    // 박스 ↔ 플레이어 인벤토리에 교환 결과를 동일하게 반영 (호스트/게스트 공통)
    private void ApplyExchangeLocally(ItemSlotUI player, int boxUid, int boxSlot, ItemData boxItem, int boxAmount, int playerSlot, ItemData playerItem, int playerAmount)
    {
        var om = ObjectManager.Instance;
        if (om != null && om.TryGet(ObjectKind.ItemBox, boxUid, out var boxObj))
        {
            var boxInv = boxObj.GetComponent<Inventory>();
            if (boxInv != null)
            {
                if (boxItem != null && boxAmount > 0) boxInv.RemoveItemAtSlot(boxSlot, boxItem, boxAmount);
                if (playerItem != null && playerAmount > 0) boxInv.AddItemAtSlot(boxSlot, playerItem, playerAmount);
            }
        }

        var playerInv = player.InventoryUI.Inventory;
        if (playerInv != null)
        {
            if (playerItem != null && playerAmount > 0) playerInv.RemoveItemAtSlot(playerSlot, playerItem, playerAmount);
            if (boxItem != null && boxAmount > 0) playerInv.AddItemAtSlot(playerSlot, boxItem, boxAmount);
        }
    }

    // 플레이어 → 플레이어 빈 슬롯: 로컬 이동만
    public void InvokeLocalMove(ItemSlotUI from, ItemSlotUI to, ItemData data, int amount)
    {
        int itemUid = from.InventoryUI.Inventory.Slots[from.SlotIndex].Uid;
        from.InventoryUI.Inventory.RemoveItemAtSlot(from.SlotIndex, data, amount);
        to.InventoryUI.Inventory.AddItemAtSlot(to.SlotIndex, data, amount, itemUid);
    }

    // 플레이어 ↔ 박스 빈 슬롯: 로컬 + 네트워크 패킷
    public void InvokeNetworkMove(ItemSlotUI from, ItemSlotUI to, ItemData data, int amount)
    {
        bool fromIsBox = from.InventoryUI.IsBox;
        int boxUid    = fromIsBox ? from.InventoryUI.Box.Id : to.InventoryUI.Box.Id;
        int boxSlotIndex = fromIsBox ? from.SlotIndex : to.SlotIndex;
        bool playerGains = fromIsBox;

        int playerSlotIndex = fromIsBox ? to.SlotIndex : from.SlotIndex;
        int itemUid = from.InventoryUI.Inventory.Slots[from.SlotIndex].Uid;

        from.InventoryUI.Inventory.RemoveItemAtSlot(from.SlotIndex, data, amount);
        to.InventoryUI.Inventory.AddItemAtSlot(to.SlotIndex, data, amount, itemUid);

        if (!RoomManager.IsHost)
        {
            // 낙관적 업데이트: 즉시 로컬 적용 후 호스트에 요청
            RoomSync.ItemGain(playerGains, boxUid, data.id, itemUid, amount, playerGains ? playerSlotIndex : boxSlotIndex, playerGains ? boxSlotIndex : playerSlotIndex);
        }
        else
        {
            RoomSync.ItemBoxUpdate(boxUid, data.id, playerGains ? -amount : amount, boxSlotIndex, itemUid);
        }
    }

    // 박스 → 박스 빈 슬롯: 로컬 이동 + 박스 업데이트 브로드캐스트
    public void InvokeBoxMove(ItemSlotUI from, ItemSlotUI to, ItemData data, int amount)
    {
        int boxUid = from.InventoryUI.Box.Id;
        int itemUid = from.InventoryUI.Inventory.Slots[from.SlotIndex].Uid;

        from.InventoryUI.Inventory.RemoveItemAtSlot(from.SlotIndex, data, amount);
        to.InventoryUI.Inventory.AddItemAtSlot(to.SlotIndex, data, amount, itemUid);

        RoomSync.ItemBoxUpdate(boxUid, data.id, -amount, from.SlotIndex);
        RoomSync.ItemBoxUpdate(boxUid, data.id, amount,  to.SlotIndex, itemUid);
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
            RoomSync.ItemBoxUpdate(boxUid, dataA.id, amountA, slotB.SlotIndex);

        if (dataB != null)
            RoomSync.ItemBoxUpdate(boxUid, dataB.id, amountB, slotA.SlotIndex);
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

    public void InvokeEquipRightClick(ItemHolderDropHandler handler)
    {
        OnEquipRightClick?.Invoke(handler);
    }

    public void InvokeItemPlaced()
    {
        OnItemPlaced?.Invoke();
    }

    public void InvokeItemPlaceFailed()
    {
        OnItemPlaceFailed?.Invoke();
    }
}
