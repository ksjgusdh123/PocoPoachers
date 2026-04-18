using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    [SerializeField] private int _maxCapacity = 50;
    [SerializeField] private int _initialCapacity = 20;

    private List<ItemSlot> _slots = new List<ItemSlot>();
    private int _currentCapacity;
    private int _itemCount = 0;

    public event Action ChangeInventory;

    public IReadOnlyList<ItemSlot> Slots => _slots;
    public int MaxCapacity => _maxCapacity;
    public int CurrentCapacity => _currentCapacity;
    public int ItemCount => _itemCount;

    private void Awake()
    {
        _currentCapacity = _initialCapacity;

        // 최대 용량만큼 슬롯 미리 생성
        for (int i = 0; i < _maxCapacity; i++)
            _slots.Add(new ItemSlot());
    }

    // 아이템 추가, 성공 여부 반환
    public bool AddItem(Item item, int amount = 1)
    {
        int remaining = amount;

        // 채워진 슬롯만 순회하여 스택 추가
        for (int i = 0; i < _itemCount; i++)
        {
            if (_slots[i].Item.Data == item.Data)
            {
                remaining = _slots[i].AddAmount(remaining);
                if (remaining <= 0)
                    return true;
            }
        }

        // 빈 슬롯(_itemCount 위치부터) 현재 용량 내에서만 추가
        while (remaining > 0 && _itemCount < _currentCapacity)
        {
            int toAdd = Mathf.Min(remaining, item.Data.MaxStack);
            _slots[_itemCount].Set(item, toAdd);
            _itemCount++;
            remaining -= toAdd;
        }

        ChangeInventory.Invoke();

        return remaining <= 0;
    }

    // 아이템 제거, 실제 제거된 수량 반환
    public int RemoveItem(ItemData itemData, int amount = 1)
    {
        int remaining = amount;
        int firstEmptyIndex = -1;

        for (int i = _itemCount - 1; i >= 0; i--)
        {
            if (_slots[i].Item.Data == itemData)
            {
                remaining -= _slots[i].RemoveAmount(remaining);
                if (_slots[i].IsEmpty)
                    firstEmptyIndex = i;

                if (remaining <= 0)
                    break;
            }
        }

        int removed = amount - remaining;
        if (removed > 0)
        {
            if (firstEmptyIndex >= 0)
                Compact(firstEmptyIndex);
        }

        return removed;
    }

    // startIndex부터 빈 슬롯을 뒤로 몰고 앞쪽을 채움
    private void Compact(int startIndex = 0)
    {
        int writeIndex = startIndex;
        for (int i = startIndex; i < _itemCount; i++)
        {
            if (!_slots[i].IsEmpty)
            {
                if (i != writeIndex)
                {
                    _slots[writeIndex].Set(_slots[i].Item, _slots[i].Amount);
                    _slots[i].Clear();
                }
                writeIndex++;
            }
        }
        _itemCount = writeIndex;
    }

    // 현재 용량 확장 (최대 용량 초과 불가)
    public void ExpandCapacity(int count)
    {
        _currentCapacity = Mathf.Min(_currentCapacity + count, _maxCapacity);
    }

    public bool HasItem(ItemData itemData, int amount = 1)
    {
        int count = 0;
        for (int i = 0; i < _itemCount; i++)
        {
            if (_slots[i].Item.Data == itemData)
                count += _slots[i].Amount;
        }
        return count >= amount;
    }
}
