using TMPro;
using UnityEngine;

public class ItemSlotUI : ItemSlotUIBase
{
    [SerializeField] private TextMeshProUGUI _enhancementText; // 장비/파츠 강화 단계 (+N), 없으면 표시 안 함
    [SerializeField] private GameObject _textBackground;       // 수량/강화 텍스트 뒷배경, 둘 다 없으면 숨김

    public InventoryUI InventoryUI { get; private set; }
    public int SlotIndex { get; private set; }
    public bool IsSettedItem { get; private set; }
    public ItemData SlotItemData => _settedSlot?.ItemData;
    public int SavedAmountItem => _settedSlot?.Amount ?? 0;
    public int SlotUid => _settedSlot?.Uid ?? 0;
    public ItemSlot Slot => _settedSlot;

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

        UpdateEnhancementText();
        UpdateTextBackground();

        // 이 슬롯을 호버 중이었다면 설명 UI도 바뀐 내용에 맞춘다
        SlotInteractionManager.GetInstance()?.RefreshHovered(this);
    }

    // 수량/강화 텍스트가 둘 다 비어 있으면 뒷배경을 끈다
    private void UpdateTextBackground()
    {
        if (_textBackground == null) return;

        bool hasAmount = _amountText != null && !string.IsNullOrEmpty(_amountText.text);
        bool hasEnhancement = _enhancementText != null && !string.IsNullOrEmpty(_enhancementText.text);
        _textBackground.SetActive(hasAmount || hasEnhancement);
    }

    // 장비/파츠면 현재 강화 단계를 "+N"으로 표시, 그 외 타입이거나 빈 슬롯이면 숨김
    private void UpdateEnhancementText()
    {
        if (_enhancementText == null) return;

        var data = _settedSlot != null && !_settedSlot.IsEmpty ? _settedSlot.ItemData : null;
        if (data == null || !IsEnhanceable(data.ItemType))
        {
            _enhancementText.text = string.Empty;
            return;
        }

        int level = WorldEquipmentManager.GetEnhancementLevel(_settedSlot.Uid, data.id);
        _enhancementText.text = $"+{level}";
    }

    private static bool IsEnhanceable(ItemType type) =>
        type == ItemType.Armor ||
        type == ItemType.Helmet ||
        type == ItemType.Weapon ||
        type == ItemType.GunPart;

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
