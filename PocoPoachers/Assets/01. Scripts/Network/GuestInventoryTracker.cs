using System.Collections.Generic;

// 호스트 전용: G_ItemGain 성공 시 게스트 인벤 슬롯을 미러링해 교환 검증에 사용
public static class GuestInventoryTracker
{
    struct SlotState
    {
        public int ItemId;
        public int Amount;
        public int Uid;
    }

    static readonly Dictionary<int, Dictionary<int, SlotState>> _guestSlots = new();

    public static void ClearGuest(int guestId) => _guestSlots.Remove(guestId);

    public static void SetSlot(int guestId, int slotIndex, int itemId, int amount, int uid = 0)
    {
        if (!_guestSlots.TryGetValue(guestId, out var slots))
        {
            slots = new Dictionary<int, SlotState>();
            _guestSlots[guestId] = slots;
        }

        if (itemId == 0 || amount <= 0)
            slots.Remove(slotIndex);
        else
            slots[slotIndex] = new SlotState { ItemId = itemId, Amount = amount, Uid = uid };
    }

    public static bool HasInSlot(int guestId, int slotIndex, int itemId, int amount)
    {
        if (amount <= 0) return true;
        if (!_guestSlots.TryGetValue(guestId, out var slots)) return false;
        if (!slots.TryGetValue(slotIndex, out var slot)) return false;
        return slot.ItemId == itemId && slot.Amount >= amount;
    }

    public static void ApplyGain(int guestId, bool takeFromBox, int playerSlot, int itemId, int amount, int uid)
    {
        if (!_guestSlots.TryGetValue(guestId, out var slots))
        {
            slots = new Dictionary<int, SlotState>();
            _guestSlots[guestId] = slots;
        }

        if (takeFromBox)
        {
            if (slots.TryGetValue(playerSlot, out var cur) && cur.ItemId == itemId)
                SetSlot(guestId, playerSlot, itemId, cur.Amount + amount, uid != 0 ? uid : cur.Uid);
            else
                SetSlot(guestId, playerSlot, itemId, amount, uid);
        }
        else if (slots.TryGetValue(playerSlot, out var cur))
        {
            int left = cur.Amount - amount;
            SetSlot(guestId, playerSlot, itemId, left, cur.Uid);
        }
    }
}
