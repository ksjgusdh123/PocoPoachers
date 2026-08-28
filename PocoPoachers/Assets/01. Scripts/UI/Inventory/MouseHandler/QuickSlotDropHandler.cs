using System;
using TMPro;
using UnityEngine;

public class QuickSlotDropHandler : ItemHolderDropHandler
{
    public static event Action<int, ItemData, int> OnQuickSlotChanged;

    [SerializeField] private TextMeshProUGUI _countText;
    private int _quickSlotCount;
    private QuickSlotInventory _quickSlotInventory;

    public void Init(InventoryUI inventoryUI, QuickSlotInventory quickSlotInventory, int quickSlotCount)
    {
        _inventoryUI = inventoryUI;
        _quickSlotInventory = quickSlotInventory;
        _quickSlotCount = quickSlotCount;
        _quickSlotInventory.GetSlot(_quickSlotCount).OnChanged += OnSlotChanged;
    }

    private void OnDestroy()
    {
        if (_quickSlotInventory == null) return;
        _quickSlotInventory.GetSlot(_quickSlotCount).OnChanged -= OnSlotChanged;
    }

    // 드래그 드롭: 인벤토리에서 아이템 꺼내 QuickSlotInventory에 저장
    protected override bool HandleDrop(SlotInteractionManager manager)
    {
        var dragged = manager.DraggedSlot;
        if (dragged == null || !dragged.IsSettedItem) return false;
        if (!IsMyInventorySlot(dragged)) return false;
        if (dragged.SlotItemData.ItemType != _itemType) return false;

        return _quickSlotInventory.TakeFrom(_quickSlotCount, dragged.SlotIndex, _inventoryUI.Inventory);
        // TakeFrom 내부에서 인벤토리 slot.Clear()가 호출되어 UI가 자동 갱신된다
    }

    // 퀵슬롯은 내 인벤토리에서만 아이템을 가져온다. TakeFrom에 넘기는 건 슬롯 번호뿐이라,
    // 상자 슬롯을 대상으로 삼으면 번호만 같은 내 인벤토리 아이템이 엉뚱하게 등록된다.
    private bool IsMyInventorySlot(ItemSlotUI slotUI) =>
        _inventoryUI != null && slotUI.InventoryUI == _inventoryUI;

    // 드래그로 넣을 때도 단축키 등록과 같은 소리를 낸다
    protected override void InvokeDropSucceeded(SlotInteractionManager manager) => manager.InvokeItemRegistered();

    // 인벤토리에서 호버 중인 아이템을 퀵슬롯에 등록 (단축키 경로)
    public bool TryRegisterItem()
    {
        var manager = SlotInteractionManager.GetInstance();
        ItemSlotUI slotUI = manager.HoveredSlot;
        // 아무것도 가리키지 않은 채 단축키만 누른 건 시도로 보지 않는다 — 소리 없이 무시
        if (slotUI == null || !slotUI.IsSettedItem) return false;

        bool registered = IsMyInventorySlot(slotUI)
            && slotUI.SlotItemData.ItemType == _itemType
            && _quickSlotInventory.TakeFrom(_quickSlotCount, slotUI.SlotIndex, _inventoryUI.Inventory);

        // 등록 성공은 전용 사운드, 실패는 일반 이동과 동일
        if (registered) manager.InvokeItemRegistered();
        else manager.InvokeItemPlaceFailed();

        return registered;
    }

    // 퀵슬롯에서 아이템 사용
    public void ConsumeItem()
    {
        _quickSlotInventory?.ConsumeItem(_quickSlotCount);
        // OnSlotChanged가 slot.OnChanged 이벤트로 자동 호출
    }

    // 퀵슬롯 해제: 남은 아이템을 인벤토리로 반납     
    public override void Unequip()
    {
        if (_inventoryUI == null) return;
        _quickSlotInventory.ReturnTo(_quickSlotCount, _inventoryUI.Inventory);
    }

    // QuickSlotInventory의 slot.OnChanged 이벤트 수신 → UI 갱신
    private void OnSlotChanged()
    {
        var slot = _quickSlotInventory.GetSlot(_quickSlotCount);

        if (slot.IsEmpty)
        {
            ClearDisplay();
            OnQuickSlotChanged?.Invoke(_quickSlotCount, null, 0);
            return;
        }

        SetDisplay(slot.ItemData, slot.Amount);
        OnQuickSlotChanged?.Invoke(_quickSlotCount, slot.ItemData, slot.Amount);
    }

    protected override void SetDisplay(ItemData data, int amount)
    {
        base.SetDisplay(data, amount);
        _countText.text = amount >= 1 ? amount.ToString() : "";
    }

    protected override void ClearDisplay()
    {
        base.ClearDisplay();
        _countText.text = "";
    }
}
