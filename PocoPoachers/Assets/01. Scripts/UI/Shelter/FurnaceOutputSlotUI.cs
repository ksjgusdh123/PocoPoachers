using UnityEngine;
using UnityEngine.EventSystems;

// 결과 칸에서 주괴를 꺼내는 조작(더블클릭 / 인벤토리로 드래그).
// 여기 있는 아이템은 인벤토리 슬롯이 아니라 화로가 직접 들고 있어서 ItemSlotUI 기반의
// 공용 드래그 경로(SlotInteractionManager)를 탈 수 없다. 그래서 자체 처리한다.
public class FurnaceOutputSlotUI : MonoBehaviour,
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private Inventory _inventory;
    private bool _dragging;

    public void Bind(Inventory inventory) => _inventory = inventory;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;
        if (eventData.clickCount != 2) return;

        TryTake();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        var furnace = Furnace.Instance;
        if (furnace == null || furnace.OutputItem == null || furnace.OutputCount <= 0) return;

        _dragging = true;
        DragIcon.Instance.Show(ResourceManager.Instance.LoadSprite(furnace.OutputItem.icon),
                               eventData.position, furnace.OutputCount);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_dragging) return;

        DragIcon.Instance.UpdatePosition(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_dragging) return;

        _dragging = false;
        DragIcon.Instance.Hide();

        // 내 인벤토리 위에 놓았을 때만 꺼낸다 — 허공에 놓은 건 취소로 본다.
        // (화로 결과물은 월드에 버리는 경로가 없으므로 그냥 화로에 남긴다)
        var target = eventData.pointerCurrentRaycast.gameObject;
        if (target == null) return;

        var ui = target.GetComponentInParent<InventoryUI>();
        if (ui == null || ui.Inventory != _inventory) return;

        TryTake();
    }

    private void TryTake()
    {
        Furnace.Instance?.TryTakeOutput(_inventory);
    }
}
