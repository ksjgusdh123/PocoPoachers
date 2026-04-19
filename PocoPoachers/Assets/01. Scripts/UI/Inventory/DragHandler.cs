using UnityEngine;
using UnityEngine.EventSystems;

// ItemSlotUI와 같은 오브젝트에 추가
public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public static ItemSlotUI SelectedItemSlot { get; private set; }
    public static CanvasGroup SelectedCanvasGroup;

    private CanvasGroup _canvasGroup;
    private ItemSlotUI _slotUI;

    public static void RemovedSelectedItemSlot()
    {
        // 선택된 아이템 슬롯이 장착되면
        DragIcon.Instance.Hide();
        SelectedItemSlot = null;

        if (SelectedCanvasGroup != null)
            SelectedCanvasGroup.alpha = 1f;
    }

    private void Awake()
    {
        _slotUI = GetComponent<ItemSlotUI>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_slotUI.IsSettedItem == false) return;

        SelectedItemSlot = _slotUI;
        SelectedCanvasGroup = _canvasGroup;
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
        SelectedCanvasGroup = null;
        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
    }

}
