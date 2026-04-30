using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private GameObject _itemVisual;

    public InventoryUI InventoryUI { get; private set; }
    public int SlotIndex { get; private set; }
    public bool IsSettedItem { get; private set; }
    public ItemData SlotItemData => _settedSlot?.ItemData;
    public int SavedAmountItem => _settedSlot.Amount;

    private ItemSlot _settedSlot;

    private void Start()
    {
        InventoryUI = GetComponentInParent<InventoryUI>();
    }

    public void SetSlot(ItemSlot slot)
    {
        _settedSlot = slot;

        if (slot.IsEmpty)
        {
            SetEmpty();
            return;
        }
        ItemData slotItemData = slot.ItemData;

        _icon.sprite = slotItemData.Icon;
        _nameText.text = slotItemData.ItemName;
        _amountText.text = slot.Amount >= 1 ? slot.Amount.ToString() : "";
        IsSettedItem = true;


        if (_itemVisual != null)
            _itemVisual.SetActive(true);
    }

    private void SetEmpty()
    {
        _icon.sprite = null;
        IsSettedItem = false;
        _nameText.text = "";
        _amountText.text = "";

        if (_itemVisual != null)
            _itemVisual.SetActive(false);
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

    public void NotifyInventoryChanged()
    {
        InventoryUI?.Refresh();
    }
}
