public class ItemSlotUI : ItemSlotUIBase
{
    public InventoryUI InventoryUI { get; private set; }
    public int SlotIndex { get; private set; }
    public bool IsSettedItem { get; private set; }
    public ItemData SlotItemData => _settedSlot?.ItemData;
    public int SavedAmountItem => _settedSlot?.Amount ?? 0;

    private ItemSlot _settedSlot;

    private void Awake()
    {
        InventoryUI = GetComponentInParent<InventoryUI>();
    }

    private void OnDestroy()
    {
        if (_settedSlot != null)
            _settedSlot.OnChanged -= Refresh;
    }

    public void SetSlot(ItemSlot slot)
    {
        if (_settedSlot != null)
            _settedSlot.OnChanged -= Refresh;

        _settedSlot = slot;
        _settedSlot.OnChanged += Refresh;
        Refresh();
    }

    private void Refresh()
    {
        if (_settedSlot.IsEmpty)
        {
            ClearDisplay();
            IsSettedItem = false;
            return;
        }

        SetDisplay(_settedSlot.ItemData, _settedSlot.Amount);
        IsSettedItem = true;
    }

    public void ClearSlot() => _settedSlot?.Clear();

    public void EquipItem(ItemData prevData, int amount)
    {
        _settedSlot.ChangeByDragDrop(prevData, amount);
    }

    public void SetSlotData(ItemData data, int amount)
    {
        if (data == null)
            _settedSlot.Clear();
        else
            _settedSlot.Set(data, amount);
    }

    public void SetIndex(int index)
    {
        SlotIndex = index;
    }
}
