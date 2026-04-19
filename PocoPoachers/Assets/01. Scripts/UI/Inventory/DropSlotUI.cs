using System.Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 장착 슬롯 공통 기반 클래스 - 상속받아 슬롯별 장착 로직 구현
public class DropSlotUI : MonoBehaviour, IDropHandler
{
    [SerializeField] ItemType _itemType;
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private GameObject _itemVisual;

    protected ItemData _droppedItemData;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (DragHandler.SelectedItemSlot == null) return;
        ItemData prev = _droppedItemData;
        if (!OnItemDropped(DragHandler.SelectedItemSlot.SlotItemData, DragHandler.SelectedItemSlot.SavedAmountItem))
        {
            _rectTransform.DOKill();
            _rectTransform.DOShakeAnchorPos(0.4f, strength: new Vector2(10f, 0f), vibrato: 20, randomness: 0);
            return;
        }
        DragHandler.SelectedItemSlot.EquipItem(prev);
    }

    protected virtual bool OnItemDropped(ItemData data, int amount)
    {
        if (data.ItemType != _itemType) return false;
        _droppedItemData = data;
        _nameText.text = data.ItemName;
        _icon.sprite = data.Icon;
        if (_itemVisual != null)
            _itemVisual.SetActive(true);
        return true;
    }

    // 장착 해제 시 호출
    public virtual void Unequip()
    {
        _droppedItemData = null;
        _nameText.text = null;
        _icon.sprite = null;
        if (_itemVisual != null)
            _itemVisual.SetActive(false);
    }
}
