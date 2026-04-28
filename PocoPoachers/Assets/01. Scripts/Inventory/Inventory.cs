using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private int _maxCapacity = 50;
    [SerializeField] private int _initialCapacity = 20;

    private List<ItemSlot> _slots = new List<ItemSlot>();
    private int _currentCapacity;

    public event Action ChangeInventory;

    public Inventory _interactionInventory {  get; set; }
    public IReadOnlyList<ItemSlot> Slots => _slots;
    public int MaxCapacity => _maxCapacity;
    public int CurrentCapacity => _currentCapacity;

    // 현재 사용 중인 슬롯 수 (갭 포함)
    public int ItemCount => CountItems();

    private void Awake()
    {
        _currentCapacity = _initialCapacity;

        // 최대 용량만큼 슬롯 미리 생성
        for (int i = 0; i < _maxCapacity; i++)
        {
            var slot = new ItemSlot();
            slot.OnCleared += () => ChangeInventory?.Invoke();
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
                if (remaining <= 0)
                {
                    ChangeInventory?.Invoke();
                    return true;
                }
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

        ChangeInventory?.Invoke();
        return remaining <= 0;
    }

    // 아이템 제거, 실제 제거된 수량 반환
    public int RemoveItem(ItemData itemData, int amount = 1)
    {
        int remaining = amount;

        for (int i = _currentCapacity - 1; i >= 0; i--)
        {
            if (!_slots[i].IsEmpty && _slots[i].ItemData == itemData)
            {
                remaining -= _slots[i].RemoveAmount(remaining);
                if (remaining <= 0)
                    break;
            }
        }

        ChangeInventory?.Invoke();
        return amount - remaining;
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

    // 아이템 타입 → 이름 순으로 정렬
    public void Sort()
    {
        // 빈 슬롯은 뒤로, 아이템 타입 → 이름 순 정렬
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
