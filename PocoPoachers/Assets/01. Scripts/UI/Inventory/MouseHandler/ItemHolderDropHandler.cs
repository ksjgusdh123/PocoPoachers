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

    public void BindInventoryUI(InventoryUI inventoryUI) => _inventoryUI = inventoryUI;

    public ItemData DroppedItemData { get; protected set; }
    public int DroppedAmount { get; protected set; }
    protected bool _isSetted;

    protected override bool HandleDrop(SlotInteractionManager manager)
    {
        ItemData prev = DroppedItemData;
        int prevAmount = DroppedAmount;
        int prevUid = GetUnequipUid(); // 덮어쓰여지기 전, 기존에 장착돼 있던 아이템의 uid
        if (!OnItemDropped(manager.DraggedSlot.SlotItemData, manager.DragAmount, manager.DraggedSlot.SlotUid))
            return false;
        if (prev == null)
            manager.DraggedSlot.ClearSlot();
        else
            manager.DraggedSlot.EquipItem(prev, prevAmount, prevUid);
        return true;
    }

    protected virtual bool OnItemDropped(ItemData data, int amount, int uid)
    {
        if (data.ItemType != _itemType) return false;

        SetDisplay(data, amount);
        return true;
    }

    // 현재 이 슬롯에 장착된 아이템 인스턴스의 uid (없으면 0) — 장비 컨트롤러를 아는 하위 클래스가 override
    protected virtual int GetUnequipUid() => 0;

    public virtual void OnPointerClick(PointerEventData eventData)
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
            _inventoryUI.Inventory.AddItemAtSlot(slot, DroppedItemData, DroppedAmount, GetUnequipUid());
        }

        ClearDisplay();
    }

    protected virtual void SetDisplay(ItemData data, int amount)
    {
        DroppedItemData = data;
        DroppedAmount = amount;
        _nameText.text = LocalizationManager.GetInstance().GetString(data.ItemName);
        _icon.sprite = ResourceManager.Instance.LoadSprite(data.icon);
        _icon.gameObject.SetActive(true);
        _isSetted = true;
    }

    protected virtual void ClearDisplay()
    {
        DroppedItemData = null;
        DroppedAmount = 0;
        _nameText.text = "";
        _icon.sprite = null;
        _icon.gameObject.SetActive(false);
        _isSetted = false;
    }
}
