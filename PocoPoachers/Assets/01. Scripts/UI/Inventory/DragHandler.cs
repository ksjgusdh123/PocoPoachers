using UnityEngine;
using UnityEngine.EventSystems;

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

        SlotInteractionManager.GetInstance().SetDragged(_slotUI, _canvasGroup);
        DragIcon.Instance.Show(_slotUI.SlotItemData.Icon, eventData.position);

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
        if (_slotUI.IsSettedItem == false) return;

        DragIcon.Instance.Hide();
        SlotInteractionManager.GetInstance().ClearDragged();

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
    }

}
