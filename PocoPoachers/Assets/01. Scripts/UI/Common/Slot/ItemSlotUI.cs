public class ItemSlotUI : ItemSlotUIBase
{
    public InventoryUI InventoryUI { get; private set; }
    public int SlotIndex { get; private set; }
    public bool IsSettedItem { get; private set; }
    public ItemData SlotItemData => _settedSlot?.ItemData;
    public int SavedAmountItem => _settedSlot?.Amount ?? 0;
    public int SlotUid => _settedSlot?.Uid ?? 0;

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
        }
        else
        {
            SetDisplay(_settedSlot.ItemData, _settedSlot.Amount);
            IsSettedItem = true;
        }

        // 이 슬롯을 호버 중이었다면 설명 UI도 바뀐 내용에 맞춘다
        SlotInteractionManager.GetInstance()?.RefreshHovered(this);
    }

    public void ClearSlot() => _settedSlot?.Clear();

    public void EquipItem(ItemData prevData, int amount, int uid = 0)
    {
        _settedSlot.ChangeByDragDrop(prevData, amount, uid);
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
