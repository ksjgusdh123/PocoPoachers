using System;
using UnityEngine;

[System.Serializable]
public class ItemSlot
{
    [SerializeField] protected ItemData _itemData;
    [SerializeField] protected int _amount;

    public event Action OnCleared;

    public ItemData ItemData => _itemData;
    public int Amount => _amount;
    public bool IsEmpty => _itemData == null;
        
    public void Set(ItemData newItemData, int newAmount)
    {
        _itemData = newItemData;
        _amount = newAmount;
    }

    public void Clear()
    {
        _itemData = null;
        _amount = 0;
        OnCleared?.Invoke();
    }

    // 이벤트 없이 초기화 (정렬 내부용)
    public void ClearSilent()
    {
        _itemData = null;
        _amount = 0;
    }

    // amount만큼 추가, 최대 스택 초과분 반환
    public int AddAmount(int value)
    {
        int maxStack = _itemData.MaxStack;
        int overflow = Mathf.Max(0, _amount + value - maxStack);
        _amount = Mathf.Min(_amount + value, maxStack);
        return overflow;
    }

    // amount만큼 제거, 실제 제거된 수량 반환
    public int RemoveAmount(int value)
    {
        int removed = Mathf.Min(_amount, value);
        _amount -= removed;
        if (_amount <= 0)
            Clear();
        return removed;
    }

    public void ChangeByDragDrop(ItemData changedData, int amount)
    {
        if (changedData == null)
            RemoveAmount(amount);
        else
        {
            Set(changedData, amount);
            OnCleared?.Invoke();
        }
    }
}
