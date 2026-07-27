using UnityEngine;
using UnityEngine.UI;

public class EquipContextMenuUI : ContextMenuUIBase
{
    [SerializeField] private Button _unequipButton;
    [SerializeField] private Button _partButton;   // 무기 슬롯일 때만 표시 — 파츠 패널 열기

    private ItemHolderDropHandler _targetHandler;

    protected override UIType UiType => UIType.EquipContextMenu;

    protected override void Awake()
    {
        base.Awake();

        _unequipButton.onClick.AddListener(OnClickUnequip);
        if (_partButton != null)
            _partButton.onClick.AddListener(OnClickPart);
    }

    protected override void Subscribe() =>
        SlotInteractionManager.GetInstance().OnEquipRightClick += ShowAt;

    protected override void Unsubscribe()
    {
        var slotManager = SlotInteractionManager.GetInstance();
        if (slotManager != null) slotManager.OnEquipRightClick -= ShowAt;
    }

    private void OnDisable()
    {
        _targetHandler = null;
    }

    private void ShowAt(ItemHolderDropHandler handler)
    {
        _targetHandler = handler;

        // "파츠 장착"은 총이 장착된 무기 슬롯에서만 노출
        if (_partButton != null)
            _partButton.gameObject.SetActive(GetTargetGun() != null);

        ShowAtPosition(handler.transform.position);
    }

    private void OnClickUnequip()
    {
        _targetHandler?.Unequip();
        Hide();
    }

    private void OnClickPart()
    {
        GunBase gun = GetTargetGun();
        if (gun != null)
            SlotInteractionManager.GetInstance().InvokeGunPartRequest(gun);
        Hide();
    }

    private GunBase GetTargetGun() => (_targetHandler as EquipDropHandler)?.GetEquippedGun();
}
