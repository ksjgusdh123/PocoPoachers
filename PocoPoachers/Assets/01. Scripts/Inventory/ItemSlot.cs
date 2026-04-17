using UnityEngine;

[System.Serializable]
public class ItemSlot
{
    [SerializeField] private Item _item;
    [SerializeField] private int _amount;

    public Item Item => _item;
    public int Amount => _amount;
    public bool IsEmpty => _item == null;

    public void Set(Item newItem, int newAmount)
    {
        _item = newItem;
        _amount = newAmount;
    }

    public void Clear()
    {
        _item = null;
        _amount = 0;
    }

    // amount만큼 추가, 최대 스택 초과분 반환
    public int AddAmount(int value)
    {
        int maxStack = _item.Data.MaxStack;
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
}
