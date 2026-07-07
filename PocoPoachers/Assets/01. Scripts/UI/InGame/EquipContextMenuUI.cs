using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class EquipContextMenuUI : UIBase
{
    [SerializeField] private Button _unequipButton;
    [SerializeField] private Button _partButton;   // 무기 슬롯일 때만 표시 — 파츠 패널 열기
    [SerializeField] private Vector2 _offset = new Vector2(100f, 0f);

    private ItemHolderDropHandler _targetHandler;
    private RectTransform _rectTransform;

    protected override UIType UiType => UIType.EquipContextMenu;

    protected override void Awake()
    {
        base.Awake();

        _rectTransform = GetComponent<RectTransform>();
        _unequipButton.onClick.AddListener(OnClickUnequip);
        if (_partButton != null)
            _partButton.onClick.AddListener(OnClickPart);

        SlotInteractionManager.GetInstance().OnEquipRightClick += ShowAt;
        UIManager.GetInstance().OnPanelClosed += HandleInventoryClosed;
    }

    // 구독 대상이 DontDestroyOnLoad 싱글톤이라, 씬 전환으로 파괴될 때 해제하지 않으면
    // 파괴된 인스턴스로 이벤트가 들어와 예외가 난다
    protected override void OnDestroy()
    {
        var slotManager = SlotInteractionManager.GetInstance();
        if (slotManager != null) slotManager.OnEquipRightClick -= ShowAt;

        var ui = UIManager.GetInstance();
        if (ui != null) ui.OnPanelClosed -= HandleInventoryClosed;

        base.OnDestroy();
    }

    private void HandleInventoryClosed(UIType type)
    {
        if (type == UIType.Inventory)
            Hide();
    }

    private void OnDisable()
    {
        _targetHandler = null;
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            if (!RectTransformUtility.RectangleContainsScreenPoint(_rectTransform, mousePos))
                Hide();
        }
    }

    private void ShowAt(ItemHolderDropHandler handler)
    {
        _targetHandler = handler;
        transform.position = handler.transform.position + (Vector3)_offset;

        // "파츠 장착"은 총이 장착된 무기 슬롯에서만 노출
        if (_partButton != null)
            _partButton.gameObject.SetActive(GetTargetGun() != null);

        Show();
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
