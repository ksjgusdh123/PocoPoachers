using UnityEngine;
using UnityEngine.UI;

// 인벤토리(착용 전) 슬롯 우클릭 메뉴 — 아이템 사용, 무기 파츠창 열기 등.
// 동작을 추가할 때는 버튼과 ShowAt의 노출 조건만 늘리면 된다.
public class InventoryContextMenuUI : ContextMenuUIBase
{
    [SerializeField] private Button _useButton;    // 소비 가능한 아이템일 때만 표시
    [SerializeField] private Button _partButton;   // 무기일 때만 표시 — 파츠 패널 열기

    private ItemSlotUI _targetSlot;

    protected override UIType UiType => UIType.InventoryContextMenu;

    protected override void Awake()
    {
        base.Awake();

        if (_useButton != null)
            _useButton.onClick.AddListener(OnClickUse);
        if (_partButton != null)
            _partButton.onClick.AddListener(OnClickPart);
    }

    protected override void Subscribe() =>
        SlotInteractionManager.GetInstance().OnInventoryRightClick += ShowAt;

    protected override void Unsubscribe()
    {
        var slotManager = SlotInteractionManager.GetInstance();
        if (slotManager != null) slotManager.OnInventoryRightClick -= ShowAt;
    }

    private void OnDisable()
    {
        _targetSlot = null;
    }

    private void ShowAt(ItemSlotUI slot)
    {
        // 내 인벤토리에서만 노출 (박스/창고 제외)
        if (slot.InventoryUI == null || slot.InventoryUI.IsBox) return;

        _targetSlot = slot;

        ItemData item = slot.SlotItemData;
        bool canUse = ItemUseSystem.CanUse(item);
        // 무기면 파츠창 열기. 게스트는 호스트에 파츠 상태를 요청해 받아온다(GunPartUI.OpenForItem)
        bool canPart = item != null && item.ItemType == ItemType.Weapon;

        if (_useButton != null)
            _useButton.gameObject.SetActive(canUse);
        if (_partButton != null)
            _partButton.gameObject.SetActive(canPart);

        // 표시할 동작이 하나도 없으면 메뉴를 띄우지 않는다
        if (!canUse && !canPart) return;

        ShowAtPosition(slot.transform.position);
    }

    private void OnClickUse()
    {
        // 퀵슬롯과 동일하게 PlayerController의 사용 시간 UI를 거쳐 소비되도록 요청만 보낸다
        if (_targetSlot != null)
            SlotInteractionManager.GetInstance()
                .InvokeInventoryItemUseRequest(_targetSlot.SlotItemData, _targetSlot.InventoryUI.Inventory);
        Hide();
    }

    private void OnClickPart()
    {
        if (_targetSlot != null && _targetSlot.SlotItemData != null)
            SlotInteractionManager.GetInstance()
                .InvokeInventoryGunPartRequest(_targetSlot.SlotItemData.id, _targetSlot.SlotUid);
        Hide();
    }
}
