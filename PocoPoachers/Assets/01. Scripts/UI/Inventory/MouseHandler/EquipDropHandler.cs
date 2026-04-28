using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 장착 슬롯 공통 기반 클래스 - 상속받아 슬롯별 장착 로직 구현
public class EquipDropHandler : BaseDropHandler
{
    [SerializeField] ItemType _itemType;
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private GameObject _itemVisual;

    public ItemType ItemType => _itemType;

    protected ItemData _droppedItemData;
    protected int _droppedAmount;

    protected override bool HandleDrop(SlotInteractionManager manager)
    {
        ItemData prev = _droppedItemData;
        int prevAmount = _droppedAmount;
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
        _droppedItemData = data;
        _droppedAmount = amount;
        _nameText.text = data.ItemName;
        _icon.sprite = data.Icon;
        _icon.gameObject.SetActive(true);
        if (_itemVisual != null)
            _itemVisual.SetActive(true);
        return true;
    }

    // 장착 해제 시 호출
    public virtual void Unequip()
    {
        _droppedItemData = null;
        _droppedAmount = 0;
        _nameText.text = "";
        _icon.sprite = null;
        if (_itemVisual != null)
            _itemVisual.SetActive(false);
    }
}
