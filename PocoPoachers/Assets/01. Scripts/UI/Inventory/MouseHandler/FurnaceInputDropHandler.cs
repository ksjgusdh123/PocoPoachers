using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// 화로 투입 슬롯. 발전기 연료 슬롯과 달리 확정 버튼이 없어 드롭 즉시 화로에 들어가고,
// 광석이 하나씩 녹으면서 줄어드는 수량 표시는 FurnaceUI가 갱신해준다.
public class FurnaceInputDropHandler : ItemHolderDropHandler
{
    [SerializeField] private TextMeshProUGUI _countText;

    // 베이스는 장비 슬롯용이라, 슬롯에 이미 뭔가 있으면 그걸 인벤토리로 되돌리는 "교환"을 한다.
    // 화로는 광석을 쌓아 담는 곳이라 되돌리면 안 되고, 나눠 든 수량만 인벤토리에서 빠져야 한다.
    protected override bool HandleDrop(SlotInteractionManager manager)
    {
        var slot = manager.DraggedSlot;
        var inventory = slot.InventoryUI != null ? slot.InventoryUI.Inventory : null;
        if (inventory == null) return false;

        ItemData data = slot.SlotItemData;
        int amount = manager.DragAmount;

        if (!OnItemDropped(data, amount, slot.SlotUid)) return false;

        inventory.RemoveItemAtSlot(slot.SlotIndex, data, amount);
        return true;
    }

    protected override bool OnItemDropped(ItemData data, int amount, int uid)
    {
        if (Furnace.Instance == null) return false;
        if (!Furnace.Instance.TryInsertOre(data, amount)) return false;

        SetDisplay(data, Furnace.Instance.InputCount);
        return true;
    }

    // 연료 슬롯과 마찬가지로 장비 컨텍스트 메뉴가 아니라 바로 인벤토리 반환으로 동작한다.
    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right) return;
        if (!IsSetted) return;

        Unequip();
    }

    // 광석을 들고 있는 주체가 슬롯이 아니라 화로이므로, 반환도 화로를 거친다.
    public override void Unequip()
    {
        if (Furnace.Instance == null || _inventoryUI == null) return;
        if (!Furnace.Instance.TryTakeInput(_inventoryUI.Inventory)) return;

        ClearDisplay();
    }

    // 화로가 제련 중이면 매 프레임 호출되므로, 바뀐 게 없으면 스프라이트 로드까지 가지 않고 끊는다.
    public void Refresh(ItemData item, int amount)
    {
        if (item == null || amount <= 0)
        {
            if (IsSetted) ClearDisplay();
            return;
        }

        if (IsSetted && DroppedItemData == item && DroppedAmount == amount) return;

        SetDisplay(item, amount);
    }

    protected override void SetDisplay(ItemData data, int amount)
    {
        base.SetDisplay(data, amount);

        if (_countText != null) _countText.text = $"x{amount}";
    }

    protected override void ClearDisplay()
    {
        base.ClearDisplay();

        if (_countText != null) _countText.text = "";
    }
}
