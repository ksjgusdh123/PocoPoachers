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

    // 패널 여닫음 소리는 sound.csv가 정한다 — UIType 이름으로 "ui_open_<uitype>" / "ui_close_<uitype>"
    // 키를 만들어 재생하고, 그 키가 테이블에 없으면 SoundManager가 조용히 넘어간다.
    // 덕분에 소리를 넣고 빼는 데 코드를 고칠 필요가 없다. 상자·창고처럼 자기 소리를 직접 내는
    // 오브젝트는 그냥 행을 만들지 않으면 된다(겹쳐 울리는 걸 막으려 인벤토리는 비워둔 상태).
    private static readonly Dictionary<UIType, string> OpenKeys = new();
    private static readonly Dictionary<UIType, string> CloseKeys = new();

    // 패널 소스는 하나뿐이라, 지금 울리는 소리가 어느 패널 것인지 기억해 둔다.
    // 발전기처럼 닫을 때 인벤토리까지 함께 닫는 도구가 있어서, 주인을 모르면 뒤이어 닫힌 패널이
    // 방금 튼 남의 닫힘 소리를 끊어버린다.
    private UIType? _panelSfxOwner;

    private void OnPanelOpened(UIType type)
    {
        if (SoundManager.GetInstance().PlayPanelSfx(KeyOf(OpenKeys, type, "ui_open_")))
            _panelSfxOwner = type;
    }

    private void OnPanelClosed(UIType type)
    {
        SoundManager sound = SoundManager.GetInstance();

        // 아직 울리는 게 이 패널의 소리일 때만 끊는다 — 닫힘 소리가 없어도 열림 소리는 여기서 멎는다.
        if (_panelSfxOwner == type)
        {
            sound.StopPanelSfx();
            _panelSfxOwner = null;
        }

        if (sound.PlayPanelSfx(KeyOf(CloseKeys, type, "ui_close_")))
            _panelSfxOwner = type;
    }

    // 조합한 키를 캐시한다 — UIType.ToString()은 호출마다 문자열을 새로 만든다.
    private static string KeyOf(Dictionary<UIType, string> cache, UIType type, string prefix)
    {
        if (cache.TryGetValue(type, out string key)) return key;

        key = prefix + type.ToString().ToLowerInvariant();
        cache[type] = key;
        return key;
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
