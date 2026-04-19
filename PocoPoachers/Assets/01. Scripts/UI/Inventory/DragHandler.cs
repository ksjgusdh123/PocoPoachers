using UnityEngine;
using UnityEngine.EventSystems;

// ItemSlotUI와 같은 오브젝트에 추가
public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static ItemSlotUI SelectedItemSlot { get; private set; }

    private ItemSlotUI _slotUI;
    private CanvasGroup _canvasGroup;


    private void Awake()
    {
        _slotUI = GetComponent<ItemSlotUI>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_slotUI.IsSettedItem == false) return;

        SelectedItemSlot = _slotUI;
        DragIcon.Instance.Show(SelectedItemSlot.SlotItemData.Icon, eventData.position);

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
        SelectedItemSlot = null;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

    }
}
