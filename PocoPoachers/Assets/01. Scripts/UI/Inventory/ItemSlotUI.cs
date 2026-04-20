using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private GameObject _itemVisual;

    public bool IsSettedItem { get; private set; }
    public ItemData SlotItemData => _settedSlot?.ItemData;
    public int SavedAmountItem => _settedSlot.Amount;

    private ItemSlot _settedSlot;

    public void OnPointerEnter(PointerEventData eventData) => SlotInteractionManager.GetInstance().SetHovered(this);
    public void OnPointerExit(PointerEventData eventData) => SlotInteractionManager.GetInstance().ClearHovered(this);

    public void SetSlot(ItemSlot slot)
    {
        if (slot.IsEmpty)
        {
            SetEmpty();
            return;
        }

        _settedSlot = slot;
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

    public void EquipItem(ItemData prevData)
    {
        _settedSlot.ChangeByDragDrop(prevData);
        DragIcon.Instance.Hide();
        SlotInteractionManager.GetInstance().ClearDragged();
    }
}
