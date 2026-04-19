using System.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 장착 슬롯 공통 기반 클래스 - 상속받아 슬롯별 장착 로직 구현
public class DropSlotUI : MonoBehaviour, IDropHandler
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _text;

    private ItemData _droppedItemData;

    public void OnDrop(PointerEventData eventData)
    {
        if (DragHandler.SelectedItemSlot == null) return;
        ItemData prev = _droppedItemData;
        OnItemDropped(DragHandler.SelectedItemSlot.SlotItemData);
        DragHandler.SelectedItemSlot.EquipItem(prev);
    }

    protected virtual void OnItemDropped(ItemData data)
    {
        _droppedItemData = data;
        _text.text = data.ItemName;
        _text.enabled = true;
        _icon.sprite = data.Icon;
        _icon.enabled = true;
    }

    // 장착 해제 시 호출
    public virtual void Unequip()
    {
        _text.text = null;
        _text.enabled = false;
        _icon.sprite = null;
        _icon.enabled = false;
    }
}
