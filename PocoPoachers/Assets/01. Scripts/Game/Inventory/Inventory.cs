using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private int _maxCapacity = 50;
    [SerializeField] private int _initialCapacity = 20;
    [SerializeField] private bool isPlayer;

    private List<ItemSlot> _slots = new List<ItemSlot>();
    private int _currentCapacity;

    public event Action ChangeInventory;
    public event Action<ItemData> OnItemAdded;

    public Inventory InteractionInventory { get; set; }
    public IReadOnlyList<ItemSlot> Slots => _slots;
    public int MaxCapacity => _maxCapacity;
    public int CurrentCapacity => _currentCapacity;

    // 현재 사용 중인 슬롯 수 (갭 포함)
    public int ItemCount => CountItems();

    private void Awake()
    {
        _currentCapacity = _initialCapacity;

        for (int i = 0; i < _maxCapacity; i++)
        {
            ItemSlot slot = isPlayer ? new ItemSlot() : new BoxItemSlot();
            _slots.Add(slot);
        }
    }

    // 아이템 추가, 성공 여부 반환
    // 해당 아이템이 있는 첫 번째 슬롯 인덱스 반환, 없으면 -1
    public int FindItemSlotIndex(ItemData itemData)
    {
        for (int i = 0; i < _currentCapacity; i++)
        {
            if (!_slots[i].IsEmpty && _slots[i].ItemData.id == itemData.id)
                return i;
        }
        return -1;
    }

    // 지정 인덱스 슬롯에 추가, 빈 슬롯이거나 같은 아이템이면 스택 추가
    public bool AddItemAtSlot(int slotIndex, ItemData itemData, int amount = 1)
    {
        if (slotIndex < 0 || slotIndex >= _currentCapacity) return false;

        var slot = _slots[slotIndex];

        if (slot.IsEmpty)
        {
            slot.Set(itemData, Mathf.Min(amount, itemData.MaxStack));
            OnItemAdded?.Invoke(itemData);
            return true;
        }

        if (slot.ItemData.id != itemData.id) return false;
        if (slot.Amount >= itemData.MaxStack) return false;

        slot.AddAmount(amount);
        OnItemAdded?.Invoke(itemData);
        return true;
    }

    // 지정 인덱스 슬롯에서 amount만큼 제거, 실제 제거된 수량 반환
    public int RemoveItemAtSlot(int slotIndex, ItemData itemData, int amount = 1)
    {
        if (slotIndex < 0 || slotIndex >= _currentCapacity) return 0;
        if (_slots[slotIndex].IsEmpty || _slots[slotIndex].ItemData.id != itemData.id) return 0;

        return _slots[slotIndex].RemoveAmount(amount);
    }

    // 현재 용량 확장 (최대 용량 초과 불가)
    public void ExpandCapacity(int count)
    {
        _currentCapacity = Mathf.Min(_currentCapacity + count, _maxCapacity);
        ChangeInventory?.Invoke();
    }

    // 용량을 count만큼 줄여도 보유 아이템(+reservedSlots개 추가 예정분)이 모두 들어가는지 확인
    public bool CanReduceCapacity(int count, int reservedSlots = 0)
    {
        int newCapacity = Mathf.Max(_currentCapacity - count, _initialCapacity);
        return ItemCount + reservedSlots <= newCapacity;
    }

    // 현재 용량 축소 (초기 용량 미만 불가)
    // 축소될 영역의 아이템은 앞쪽 빈 슬롯으로 이동, 자리가 없으면 접근 불가 상태로 남음
    public void ReduceCapacity(int count)
    {
        int newCapacity = Mathf.Max(_currentCapacity - count, _initialCapacity);

        for (int i = newCapacity; i < _currentCapacity; i++)
        {
            if (_slots[i].IsEmpty) continue;
            for (int j = 0; j < newCapacity; j++)
            {
                if (!_slots[j].IsEmpty) continue;
                _slots[j].Set(_slots[i].ItemData, _slots[i].Amount);
                _slots[i].Clear();
                break;
            }
        }

        _currentCapacity = newCapacity;
        ChangeInventory?.Invoke();
    }

    // 모든 슬롯을 비운다 (사망 등으로 인벤토리 전체 삭제 시)
    public void Clear()
    {
        for (int i = 0; i < _slots.Count; i++)
            _slots[i].Clear();
        ChangeInventory?.Invoke();
    }

    // 추가 가능한 첫 번째 슬롯 인덱스 반환, 불가능하면 -1
    public int CanAddItem(ItemData itemData, int amount = 1)
    {
        int remaining = amount;
        int firstAvailableIndex = -1;

        for (int i = 0; i < _currentCapacity; i++)
        {
            if (_slots[i].IsEmpty)
            {
                if (firstAvailableIndex < 0) firstAvailableIndex = i;
                remaining -= itemData.MaxStack;
            }
            else if (_slots[i].ItemData == itemData)
            {
                if (firstAvailableIndex < 0) firstAvailableIndex = i;
                remaining -= itemData.MaxStack - _slots[i].Amount;
            }

            if (remaining <= 0) return firstAvailableIndex;
        }

        return -1;
    }

    public bool HasItem(ItemData itemData, int amount = 1)
    {
        return GetItemCount(itemData) >= amount;
    }

    public int GetItemCount(ItemData itemData)
    {
        int count = 0;
        for (int i = 0; i < _currentCapacity; i++)
        {
            if (!_slots[i].IsEmpty && _slots[i].ItemData == itemData)
                count += _slots[i].Amount;
        }
        return count;
    }

    // 여러 슬롯에 걸쳐 아이템 제거, 실제 제거된 수량 반환
    public int RemoveItem(ItemData itemData, int amount)
    {
        int remaining = amount;
        for (int i = 0; i < _currentCapacity && remaining > 0; i++)
        {
            if (_slots[i].IsEmpty || _slots[i].ItemData != itemData) continue;
            remaining -= _slots[i].RemoveAmount(remaining);
        }
        ChangeInventory?.Invoke();
        return amount - remaining;
    }

    public void SwapSlots(int indexA, int indexB)
    {
        if (indexA < 0 || indexA >= _currentCapacity) return;
        if (indexB < 0 || indexB >= _currentCapacity) return;

        ItemData dataA = _slots[indexA].ItemData;
        int amountA = _slots[indexA].Amount;
        ItemData dataB = _slots[indexB].ItemData;
        int amountB = _slots[indexB].Amount;

        _slots[indexA].Clear();
        _slots[indexB].Clear();

        if (dataB != null) _slots[indexA].Set(dataB, amountB);
        if (dataA != null) _slots[indexB].Set(dataA, amountA);
    }

    // 아이템 타입 → 이름 순으로 정렬
    public void Sort()
    {
        _slots.Sort(0, _currentCapacity, Comparer<ItemSlot>.Create((a, b) =>
        {
            if (a.IsEmpty && b.IsEmpty) return 0;
            if (a.IsEmpty) return 1;
            if (b.IsEmpty) return -1;

            int typeCompare = a.ItemData.ItemType.CompareTo(b.ItemData.ItemType);
            return typeCompare != 0 ? typeCompare : string.Compare(a.ItemData.ItemName, b.ItemData.ItemName);
        }));

        ChangeInventory?.Invoke();
    }

    private int CountItems()
    {
        int count = 0;
        for (int i = 0; i < _currentCapacity; i++)
        {
            if (!_slots[i].IsEmpty) count++;
        }
        return count;
    }
}
