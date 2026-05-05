using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class BaseDropHandler : MonoBehaviour, IDropHandler
{
    [SerializeField] protected ItemType _itemType;
    [SerializeField] protected Image _icon;
    [SerializeField] protected TextMeshProUGUI _nameText;

    public ItemType ItemType => _itemType;
    public bool IsSetted => _isSetted;

    public ItemData _droppedItemData { get; protected set; }
    public int _droppedAmount { get; protected set; }
    protected bool _isSetted;

    private RectTransform _rectTransform;

    protected virtual void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        var manager = SlotInteractionManager.GetInstance();
        if (manager.DraggedSlot == null) return;
        if (!HandleDrop(manager))
        {
            _rectTransform.DOKill();
            _rectTransform.DOShakeAnchorPos(0.4f, strength: new Vector2(10f, 0f), vibrato: 20, randomness: 0);
        }
    }

    protected virtual bool HandleDrop(SlotInteractionManager manager)
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
        _isSetted = true;
        return true;
    }

    public virtual void Unequip()
    {
        _droppedItemData = null;
        _droppedAmount = 0;
        _nameText.text = "";
        _icon.sprite = null;
        _icon.gameObject.SetActive(false);
        _isSetted = false;
    }
}
