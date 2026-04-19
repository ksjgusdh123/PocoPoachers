using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private GameObject _emptyOverlay;

    public bool IsSettedItem { get; private set; }
    public ItemData SlotItemData => settedSlot.Item.Data;

    private ItemSlot settedSlot;

    public void SetSlot(ItemSlot slot)
    {
        if (slot.IsEmpty)
        {
            SetEmpty();
            return;
        }

        settedSlot = slot;
        ItemData slotItemData = slot.Item.Data;

        _icon.sprite = slotItemData.Icon;
        _icon.enabled = true;
        _nameText.text = slotItemData.ItemName;
        _amountText.text = slot.Amount >= 1 ? slot.Amount.ToString() : "";
        IsSettedItem = true;

        if (_emptyOverlay != null)
            _emptyOverlay.SetActive(false);
    }

    private void SetEmpty()
    {
        _icon.sprite = null;
        IsSettedItem = false;
        //_icon.enabled = false;
        _nameText.text = "";
        _amountText.text = "";

        if (_emptyOverlay != null)
            _emptyOverlay.SetActive(true);
    }

    public void EquipItem()
    {
    }
}
