using System;
using TMPro;
using UnityEngine;

public class QuickSlotDropHandler : ItemHolderDropHandler
{
    public static event Action<int, ItemData, int> OnQuickSlotChanged;

    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private int _quickSlotCount;
    [SerializeField] private QuickSlotInventory _quickSlotInventory;

    private void Start()
    {
        // TODO : 시작할 때 자기 인벤찾아야함
        // TEMP
        _quickSlotInventory = FindAnyObjectByType<QuickSlotInventory>();
        _quickSlotInventory.GetSlot(_quickSlotCount).OnChanged += OnSlotChanged;
    }

    // 드래그 드롭: 인벤토리에서 아이템 꺼내 QuickSlotInventory에 저장
    protected override bool HandleDrop(SlotInteractionManager manager)
    {
        var dragged = manager.DraggedSlot;
        if (dragged == null || !dragged.IsSettedItem) return false;
        if (dragged.SlotItemData.ItemType != _itemType) return false;
        if (_inventoryUI == null) return false;

        return _quickSlotInventory.TakeFrom(_quickSlotCount, dragged.SlotIndex, _inventoryUI.Inventory);
        // TakeFrom 내부에서 인벤토리 slot.Clear()가 호출되어 UI가 자동 갱신된다
    }

    // 인벤토리에서 호버 중인 아이템을 퀵슬롯에 등록 (단축키 경로)
    public bool TryRegisterItem()
    {
        ItemSlotUI slotUI = SlotInteractionManager.GetInstance().HoveredSlot;
        if (slotUI == null || !slotUI.IsSettedItem) return false;
        if (slotUI.SlotItemData.ItemType != _itemType) return false;
        if (_inventoryUI == null) return false;

        return _quickSlotInventory.TakeFrom(_quickSlotCount, slotUI.SlotIndex, _inventoryUI.Inventory);
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
