using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// ItemSlotUI와 같은 오브젝트에 추가
public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup _canvasGroup;
    private ItemSlotUI _slotUI;

    private void Awake()
    {
        _slotUI = GetComponent<ItemSlotUI>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_slotUI.IsSettedItem == false) return;
        if (eventData.button != PointerEventData.InputButton.Left) return;

            var manager = SlotInteractionManager.GetInstance();
        int amount;

        if (manager.PendingSlot == _slotUI && manager.PendingAmount > 0)
        {
            amount = manager.PendingAmount;
            manager.ResetPending();
        }
        else
            amount = _slotUI.SavedAmountItem;

        manager.SetDragged(_slotUI, _canvasGroup, amount);
        DragIcon.Instance.Show(ResourceManager.Instance.LoadSprite(_slotUI.SlotItemData.icon), eventData.position, amount);

        if (_canvasGroup != null)
            _canvasGroup.alpha = 0.4f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_slotUI.IsSettedItem == false) return;

        DragIcon.Instance.UpdatePosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
        SlotInteractionManager.GetInstance().ClearDragged();
    }

}
