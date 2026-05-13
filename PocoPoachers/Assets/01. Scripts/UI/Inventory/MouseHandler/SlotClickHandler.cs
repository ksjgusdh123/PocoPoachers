using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// ItemSlotUI와 같은 오브젝트에 추가
public class SlotClickHandler : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    private ItemSlotUI _slotUI;

    private void Awake()
    {
        _slotUI = GetComponent<ItemSlotUI>();
    }

    public void OnPointerEnter(PointerEventData eventData) => SlotInteractionManager.GetInstance().SetHovered(_slotUI);

    public void OnPointerExit(PointerEventData eventData)
    {
        var manager = SlotInteractionManager.GetInstance();
        manager.ClearHovered(_slotUI);
        if (manager.PendingSlot == _slotUI)
        {
            manager.ResetPending();
            DragIcon.Instance.Hide();
        }
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        // 더블클릭: Unity UI clickCount 활용
        if (eventData.clickCount == 2 && eventData.button == PointerEventData.InputButton.Left)
        {
            SlotInteractionManager.GetInstance().InvokeDoubleClick();
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (!_slotUI.IsSettedItem) return;

        var keyboard = Keyboard.current;
        var manager = SlotInteractionManager.GetInstance();

        if (keyboard.ctrlKey.isPressed)
        {
            manager.IncrementPending(_slotUI);
            DragIcon.Instance.Show(_slotUI.SlotItemData.Icon, eventData.position, manager.PendingAmount);
        }
        else if (keyboard.shiftKey.isPressed)
        {
            manager.SetPending(_slotUI, _slotUI.SavedAmountItem / 2);
            DragIcon.Instance.Show(_slotUI.SlotItemData.Icon, eventData.position, manager.PendingAmount);
        }
    }
}
