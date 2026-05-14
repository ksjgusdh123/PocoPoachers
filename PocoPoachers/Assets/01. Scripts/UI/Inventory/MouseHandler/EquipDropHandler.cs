using UnityEngine;
using UnityEngine.EventSystems;

public class EquipDropHandler : ItemHolderDropHandler, IPointerClickHandler
{
    [SerializeField] private GameObject _itemVisual;
    [SerializeField] private int _slotIndex;
    [SerializeField] private EquipableController _controller;
    [SerializeField] private InventoryUI _inventoryUI;


    protected override bool OnItemDropped(ItemData data, int amount)
    {
        if (!base.OnItemDropped(data, amount)) return false;

        if (_controller == null)
        {
            Debug.LogWarning($"[EquipDropHandler] {gameObject.name}에 Controller가 할당되지 않았습니다.");
            return false;
        }

        _controller.Equip(data, _slotIndex);

        if (_itemVisual != null)
            _itemVisual.SetActive(true);
        return true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"[EquipDropHandler] 클릭 감지: {eventData.button}, isSetted={_isSetted}");
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (!_isSetted) return;

        SlotInteractionManager.GetInstance().InvokeEquipRightClick(this, eventData.position);
    }

    public override void Unequip()
    {
        if (DroppedItemData != null && _inventoryUI != null)
        {
            int slot = _inventoryUI.Inventory.CanAddItem(DroppedItemData, DroppedAmount);
            if (slot < 0) return;
            _inventoryUI.Inventory.AddItemAtSlot(slot, DroppedItemData, DroppedAmount);
        }

        base.Unequip();
        _controller?.Unequip(_slotIndex);
        if (_itemVisual != null)
            _itemVisual.SetActive(false);
    }
}
