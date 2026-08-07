using System.Collections.Generic;
using UnityEngine;

// 퀵슬롯 전용 저장소
// 아이템을 플레이어 인벤토리에서 꺼내 보관하고, 사용 또는 해제 시 반납한다
public class QuickSlotInventory : MonoBehaviour
{
    [SerializeField] private int _slotCount = 6;
    [SerializeField] private int _startIndex = 3; // 퀵슬롯 카운트 시작 번호

    private ItemSlot[] _slots;

    public int SlotCount => _slotCount;
    public int StartIndex => _startIndex;

    private void Awake()
    {
        _slots = new ItemSlot[_slotCount];
        for (int i = 0; i < _slotCount; i++)
            _slots[i] = new ItemSlot();
    }

    public ItemSlot GetSlot(int index) => _slots[ToLocal(index)];

    // 플레이어 인벤토리의 특정 슬롯에서 아이템 전체를 퀵슬롯으로 이동
    public bool TakeFrom(int quickSlotIndex, int inventorySlotIndex, Inventory inventory)
    {
        if (!IsValidIndex(quickSlotIndex)) return false;

        var invSlot = inventory.Slots[inventorySlotIndex];
        if (invSlot.IsEmpty) return false;

        ItemData data = invSlot.ItemData;
        int amount = invSlot.Amount;
        int uid = invSlot.Uid;

        // 슬롯에 기존 아이템이 있으면 먼저 인벤토리로 반납
        ReturnTo(quickSlotIndex, inventory);

        inventory.RemoveItemAtSlot(inventorySlotIndex, data, amount);
        _slots[ToLocal(quickSlotIndex)].Set(data, amount, uid);
        return true;
    }

    // 퀵슬롯 아이템을 플레이어 인벤토리로 반납
    // 반납 공간이 없으면 false 반환 (아이템 유지)
    public bool ReturnTo(int quickSlotIndex, Inventory inventory)
    {
        if (!IsValidIndex(quickSlotIndex)) return false;

        var slot = _slots[ToLocal(quickSlotIndex)];
        if (slot.IsEmpty) return true;

        int addIdx = inventory.CanAddItem(slot.ItemData, slot.Amount);
        if (addIdx < 0) return false;

        inventory.AddItemAtSlot(addIdx, slot.ItemData, slot.Amount, slot.Uid);
        slot.Clear();
        return true;
    }

    // 아이템 사용: 효과 적용 후 수량 차감
    // slot.OnChanged 이벤트가 발생해 QuickSlotDropHandler UI가 자동 갱신된다
    public bool ConsumeItem(int quickSlotIndex)
    {
        if (!IsValidIndex(quickSlotIndex)) return false;

        var slot = _slots[ToLocal(quickSlotIndex)];
        if (slot.IsEmpty) return false;
        ItemData usedItemData = slot.ItemData;
        if (!ItemUseSystem.CanUse(usedItemData)) return false;

        ItemUseSystem.ApplyEffect(usedItemData);
        slot.RemoveAmount(1);

        if (!RoomManager.IsHost)
        {
            PacketBuilder.SendReliableToHost(new G_ConsumeItemT
            {
                ItemId = usedItemData.id,
                Amount = 1
            }, G_ConsumeItem.Pack, PacketType.G_ConsumeItem);
        }
        else
        {
            PacketBuilder.BroadcastToGuests(new H_ConsumeItemResultT
            {
                PlayerId = NetworkManager.Instance?.MyPlayerId ?? 0,
                ItemId   = usedItemData.id,
            }, H_ConsumeItemResult.Pack, PacketType.H_ConsumeItemResult);
        }
        return true;
    }

    // 퀵슬롯 아이템은 인벤토리에서 빠져나와 있어 인벤 세이브에 안 잡힌다 — 따로 저장하지 않으면 맵 이동 시 소실된다
    public List<SaveManager.SlotSaveEntry> Export()
    {
        var entries = new List<SaveManager.SlotSaveEntry>();
        for (int i = 0; i < _slots.Length; i++)
        {
            var slot = _slots[i];
            if (slot.IsEmpty) continue;

            entries.Add(new SaveManager.SlotSaveEntry
            {
                slotIndex = _startIndex + i,
                itemId    = slot.ItemData.id,
                amount    = slot.Amount,
                uid       = slot.Uid,
            });
        }
        return entries;
    }

    // QuickSlotDropHandler.Init으로 UI가 slot.OnChanged를 구독한 뒤에 호출해야 표시가 갱신된다
    public void Import(List<SaveManager.SlotSaveEntry> entries)
    {
        if (entries == null) return;

        foreach (var entry in entries)
        {
            if (!IsValidIndex(entry.slotIndex)) continue;

            ItemData data = ItemTable.Instance.Get(entry.itemId);
            if (data == null) continue;

            _slots[ToLocal(entry.slotIndex)].Set(data, entry.amount, entry.uid);
        }
    }

    public void Clear()
    {
        foreach (var slot in _slots)
            if (!slot.IsEmpty) slot.Clear();
    }

    // 외부 인덱스(3,4,5...) → 내부 배열 인덱스(0,1,2...)
    private int ToLocal(int index) => index - _startIndex;

    private bool IsValidIndex(int index)
    {
        int local = ToLocal(index);
        return local >= 0 && local < _slots.Length;
    }
}
