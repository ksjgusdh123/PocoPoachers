using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class ItemHolderDropHandler : BaseDropHandler, IPointerClickHandler
{
    [SerializeField] protected ItemType _itemType;
    [SerializeField] protected Image _icon;
    [SerializeField] protected TextMeshProUGUI _nameText;
    [SerializeField] protected InventoryUI _inventoryUI;

    public ItemType ItemType => _itemType;
    public bool IsSetted => _isSetted;

    public ItemData DroppedItemData { get; protected set; }
    public int DroppedAmount { get; protected set; }
    protected bool _isSetted;

    protected override bool HandleDrop(SlotInteractionManager manager)
    {
        ItemData prev = DroppedItemData;
        int prevAmount = DroppedAmount;
        if (!OnItemDropped(manager.DraggedSlot.SlotItemData, manager.DragAmount))
            return false;
        if (prev == null)
            manager.DraggedSlot.ClearSlot();
        else
            manager.DraggedSlot.EquipItem(prev, prevAmount);
        return true;
    }

    protected virtual bool OnItemDropped(ItemData data, int amount)
    {
        if (data.ItemType != _itemType) return false;

        DroppedItemData = data;
        DroppedAmount = amount;
        _nameText.text = data.ItemName;
        _icon.sprite = data.Icon;
        _icon.gameObject.SetActive(true);
        _isSetted = true;
        return true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (!_isSetted) return;

        SlotInteractionManager.GetInstance().InvokeEquipRightClick(this);
    }

    public virtual void Unequip()
    {
        if (DroppedItemData != null && _inventoryUI != null)
        {
            int slot = _inventoryUI.Inventory.CanAddItem(DroppedItemData, DroppedAmount);
            if (slot < 0) return;
            _inventoryUI.Inventory.AddItemAtSlot(slot, DroppedItemData, DroppedAmount);
        }

        DroppedItemData = null;
        DroppedAmount = 0;
        _nameText.text = "";
        _icon.sprite = null;
        _icon.gameObject.SetActive(false);
        _isSetted = false;
    }
}
