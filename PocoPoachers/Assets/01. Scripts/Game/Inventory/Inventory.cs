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
    public bool AddItem(ItemData itemData, int amount = 1)
    {
        int remaining = amount;

        // 같은 아이템 슬롯에 스택 추가
        for (int i = 0; i < _currentCapacity; i++)
        {
            if (!_slots[i].IsEmpty && _slots[i].ItemData == itemData)
            {
                remaining = _slots[i].AddAmount(remaining);
                if (remaining <= 0) return true;
            }
        }

        // 첫 번째 빈 슬롯에 추가
        for (int i = 0; i < _currentCapacity && remaining > 0; i++)
        {
            if (_slots[i].IsEmpty)
            {
                int toAdd = Mathf.Min(remaining, itemData.MaxStack);
                _slots[i].Set(itemData, toAdd);
                remaining -= toAdd;
            }
        }

        return remaining <= 0;
    }

    // 아이템 제거, 실제 제거된 수량 반환
    public int RemoveItem(ItemData itemData, int amount = 1)
    {
        int remaining = amount;

        for (int i = _currentCapacity - 1; i >= 0; i--)
        {
            if (!_slots[i].IsEmpty && _slots[i].ItemData.id == itemData.id)
            {
                remaining -= _slots[i].RemoveAmount(remaining);
                if (remaining <= 0) break;
            }
        }

        return amount - remaining;
    }

    // 지정 인덱스 슬롯이 비어있으면 추가, 비어있지 않으면 false 반환
    public bool AddItemAtSlot(int slotIndex, ItemData itemData, int amount = 1)
    {
        if (slotIndex < 0 || slotIndex >= _currentCapacity) return false;
        if (!_slots[slotIndex].IsEmpty) return false;

        int toAdd = Mathf.Min(amount, itemData.MaxStack);
        _slots[slotIndex].Set(itemData, toAdd);
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

    public bool CanAddItem(ItemData itemData, int amount = 1)
    {
        int remaining = amount;

        for (int i = 0; i < _currentCapacity; i++)
        {
            if (_slots[i].IsEmpty)
                remaining -= itemData.MaxStack;
            else if (_slots[i].ItemData == itemData)
                remaining -= itemData.MaxStack - _slots[i].Amount;

            if (remaining <= 0) return true;
        }

        return false;
    }

    public bool HasItem(ItemData itemData, int amount = 1)
    {
        int count = 0;
        for (int i = 0; i < _currentCapacity; i++)
        {
            if (!_slots[i].IsEmpty && _slots[i].ItemData == itemData)
                count += _slots[i].Amount;
        }
        return count >= amount;
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
