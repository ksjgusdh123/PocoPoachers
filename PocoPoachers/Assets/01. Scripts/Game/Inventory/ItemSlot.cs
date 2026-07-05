using System;
using UnityEngine;

[System.Serializable]
public class ItemSlot
{
    [SerializeField] protected ItemData _itemData;
    [SerializeField] protected int _amount;
    [SerializeField] protected int _uid; // 0 = 미배정 (스택형 소모품 등은 사용 안 함)

    public event Action OnChanged;

    public ItemData ItemData => _itemData;
    public int Amount => _amount;
    public int Uid => _uid;
    public bool IsEmpty => _itemData == null;

    public void SetUid(int uid)
    {
        _uid = uid;
    }

    // uid를 생략하면 0(미배정)으로 설정됨 — 같은 아이템의 uid를 유지하려면 호출부에서 명시적으로 넘겨야 함
    public void Set(ItemData newItemData, int newAmount, int uid = 0)
    {
        _itemData = newItemData;
        _amount = newAmount;
        _uid = uid;
        OnChanged?.Invoke();
    }

    public void Clear()
    {
        _itemData = null;
        _amount = 0;
        _uid = 0;
        OnChanged?.Invoke();
    }

    // 이벤트 없이 초기화 (정렬 내부용)
    public void ClearSilent()
    {
        _itemData = null;
        _amount = 0;
        _uid = 0;
    }

    // amount만큼 추가, 최대 스택 초과분 반환
    public int AddAmount(int value)
    {
        int maxStack = _itemData.MaxStack;
        int overflow = Mathf.Max(0, _amount + value - maxStack);
        _amount = Mathf.Min(_amount + value, maxStack);
        OnChanged?.Invoke();
        return overflow;
    }

    // amount만큼 제거, 실제 제거된 수량 반환
    public int RemoveAmount(int value)
    {
        int removed = Mathf.Min(_amount, value);
        _amount -= removed;
        if (_amount <= 0)
            Clear();
        else
            OnChanged?.Invoke();
        return removed;
    }

    public void ChangeByDragDrop(ItemData changedData, int amount, int uid = 0)
    {
        if (changedData == null)
            RemoveAmount(amount);
        else
            Set(changedData, amount, uid);
    }
}
