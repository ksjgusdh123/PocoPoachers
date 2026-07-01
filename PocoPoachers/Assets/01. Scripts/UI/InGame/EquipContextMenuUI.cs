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
