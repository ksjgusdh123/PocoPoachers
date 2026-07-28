using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// ItemSlotUI와 같은 오브젝트에 추가
public class DragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private CanvasGroup _canvasGroup;
    private ItemSlotUI _slotUI;
    private int _draggedAmount;   // 이번 드래그로 집어든 수량 — UI 밖에 버릴 때 이 수량만 버린다

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

        _draggedAmount = amount;
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

        // 드롭 지점이 UI(RectTransform) 위가 아니면(허공/월드) 월드에 상자로 버린다.
        // 슬롯 위에 놓았으면 그 전에 InventoryDropHandler.OnDrop이 이동을 처리하고, 여기선 UI 위라 버리지 않는다.
        // 월드 콜라이더가 레이캐스트에 잡혀도 RectTransform이 없으므로 버림 처리된다.
        var dropTarget = eventData.pointerCurrentRaycast.gameObject;
        bool overUI = dropTarget != null && dropTarget.GetComponent<RectTransform>() != null;
        if (!overUI)
            DiscardToWorld();

        SlotInteractionManager.GetInstance().ClearDragged();
    }

    // 플레이어 인벤토리의 이 슬롯 아이템(드래그한 수량)을 월드에 상자로 버린다 (호스트/솔로만 실제 동작)
    private void DiscardToWorld()
    {
        Inventory inventory = _slotUI.InventoryUI != null ? _slotUI.InventoryUI.Inventory : null;
        if (inventory == null || !inventory.IsPlayer) return;   // 내 인벤에서만 (박스/창고 제외)

        var dropper = inventory.GetComponent<PlayerItemBoxDropper>();
        if (dropper != null)
            dropper.DropSlotToWorld(_slotUI.SlotIndex, _draggedAmount);
    }

}
