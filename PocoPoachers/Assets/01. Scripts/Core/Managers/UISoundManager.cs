using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UISoundManager : Singleton<UISoundManager>
{
    private const string DefaultHoverKey = "ui_hover";

    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();
    private Button _hoveredButton;

    // 호버 대상이 바뀔 때만 갱신한다 — 포인터가 움직이는 매 프레임 GetComponent를 부르지 않도록.
    private UIButtonSound _hoveredButtonSound;
    private SlotInteractionManager _slotInteractionManager;
    private UIManager _uiManager;

    protected override void Awake()
    {
        base.Awake();

        _slotInteractionManager = SlotInteractionManager.GetInstance();
        _slotInteractionManager.OnDragBegin += OnSlotDragBegin;
        _slotInteractionManager.OnItemPlaced += OnItemPlaced;
        _slotInteractionManager.OnItemPlaceFailed += OnItemPlaceFailed;
        _slotInteractionManager.OnItemRegistered += OnItemRegistered;

        _uiManager = UIManager.GetInstance();
        _uiManager.OnPanelOpened += OnPanelOpened;
        _uiManager.OnPanelClosed += OnPanelClosed;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (_slotInteractionManager != null)
        {
            _slotInteractionManager.OnDragBegin -= OnSlotDragBegin;
            _slotInteractionManager.OnItemPlaced -= OnItemPlaced;
            _slotInteractionManager.OnItemPlaceFailed -= OnItemPlaceFailed;
            _slotInteractionManager.OnItemRegistered -= OnItemRegistered;
        }

        if (_uiManager != null)
        {
            _uiManager.OnPanelOpened -= OnPanelOpened;
            _uiManager.OnPanelClosed -= OnPanelClosed;
        }
    }

    private void OnSlotDragBegin(ItemSlotUI _) => SoundManager.GetInstance().PlaySfx("ui_slot_click");

    private void OnItemPlaced() => SoundManager.GetInstance().PlaySfx("ui_item_place");

    private void OnItemPlaceFailed() => SoundManager.GetInstance().PlaySfx("ui_item_place_fail");

    private void OnItemRegistered() => SoundManager.GetInstance().PlaySfx("ui_item_register");

    private void OnPanelOpened(UIType type)
    {
        if (type == UIType.Inventory) SoundManager.GetInstance().PlaySfx("ui_inventory_open");
    }

    private void OnPanelClosed(UIType type)
    {
        if (type == UIType.Inventory) SoundManager.GetInstance().PlaySfx("ui_inventory_close");
    }

    // 호버 사운드를 위해 UI 레이캐스트가 필요하지만, 매 프레임 전체 레이캐스트 + PointerEventData
    // 할당은 낭비다. 포인터가 실제로 움직였거나 클릭이 발생한 프레임에만 검사하고,
    // PointerEventData는 하나를 재사용한다.
    private const float PointerMoveThresholdSqr = 0.01f;

    private PointerEventData _pointerData;
    private EventSystem _pointerDataOwner;
    private Vector2 _lastPointerPosition;
    private bool _hasLastPointerPosition;

    private void Update()
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null || Mouse.current == null) return;

        Vector2 position = Mouse.current.position.ReadValue();
        bool clicked = Mouse.current.leftButton.wasPressedThisFrame;
        bool moved = !_hasLastPointerPosition
            || (position - _lastPointerPosition).sqrMagnitude > PointerMoveThresholdSqr;

        if (!moved && !clicked) return;

        _lastPointerPosition = position;
        _hasLastPointerPosition = true;

        Button button = RaycastButton(eventSystem, position);

        if (button != _hoveredButton)
        {
            _hoveredButton = button;
            _hoveredButtonSound = button != null ? button.GetComponent<UIButtonSound>() : null;

            if (button != null)
                SoundManager.GetInstance().PlaySfx(HoverKeyOf(_hoveredButtonSound));
        }

        // 클릭 대상은 방금 호버 판정을 마친 그 버튼이라 캐시한 설정을 그대로 쓴다
        if (clicked && button != null)
            PlayClick(_hoveredButtonSound);
    }

    // 버튼에 UIButtonSound가 붙어 있고 키가 채워져 있으면 그 소리로 바꾼다
    private static string HoverKeyOf(UIButtonSound custom) =>
        custom != null && !string.IsNullOrEmpty(custom.HoverKey) ? custom.HoverKey : DefaultHoverKey;

    private static void PlayClick(UIButtonSound custom)
    {
        if (custom != null && !string.IsNullOrEmpty(custom.ClickKey))
            SoundManager.GetInstance().PlaySfx(custom.ClickKey);
        else
            SoundManager.GetInstance().PlayButtonClick();
    }

    private Button RaycastButton(EventSystem eventSystem, Vector2 position)
    {
        if (_pointerData == null || _pointerDataOwner != eventSystem)
        {
            _pointerData = new PointerEventData(eventSystem);
            _pointerDataOwner = eventSystem;
        }

        _pointerData.Reset();
        _pointerData.position = position;

        _raycastResults.Clear();
        eventSystem.RaycastAll(_pointerData, _raycastResults);
        if (_raycastResults.Count == 0) return null;

        var hit = _raycastResults[0].gameObject.GetComponentInParent<Button>();
        return hit != null && hit.interactable ? hit : null;
    }
}
