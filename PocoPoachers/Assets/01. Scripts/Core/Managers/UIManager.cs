using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public enum UIType
{
    Inventory,
    Storage,
    EnhancementTable,
    GunEnhancementTable,
    RepairWorkbench,
    ItemBoxReveal,
    EquipContextMenu,
    IngameMenu,
    WarningPopup,
    NoticePopup,
    JoinCode,
    Options,
    PlanetSelect,
    ShelterUpgrade,
    CraftingTable,
    MainGameUI,
    Generator,
    InventoryContextMenu,
    Dialogue,
    Quest,
    QuestNotice,
    Minimap,
}

public class UIManager : Singleton<UIManager>
{
    // 패널 정렬 순서 기준값. 열림 순서대로 이 값부터 1씩 올려 배정한다.
    [SerializeField] private int _panelSortingOrderBase = 100;

    // 드래그 아이콘처럼 항상 모든 패널 위에 떠야 하는 요소가 쓰는 정렬 순서.
    public const int OverlaySortingOrder = 1000;

    [Header("Input")]
    [SerializeField]
    private InputAction _cancelAction = new InputAction("UICancel", InputActionType.Button, "<Keyboard>/escape");

    [Header("Open Animation")]
    [SerializeField, Range(0f, 0.5f), Tooltip("0이면 연출을 끈다.")]
    private float _openAnimationDuration = 0.12f;

    [SerializeField, Range(0.5f, 1f), Tooltip("열릴 때 시작 스케일 배율")]
    private float _openAnimationScaleFrom = 0.94f;

    [SerializeField, Range(0f, 0.5f), Tooltip("닫힐 때 페이드아웃 시간. 0이면 즉시 닫는다.")]
    private float _closeAnimationDuration = 0.08f;

    // 닫힘 연출이 진행 중인 패널. 연출 도중 다시 열리면 취소해야 한다.
    private readonly HashSet<GameObject> _closing = new();

    // 패널의 원래 스케일. 연출이 중간에 끊겨도 원래 값으로 되돌릴 수 있게 기억한다.
    private readonly Dictionary<RectTransform, Vector3> _panelBaseScales = new();

    private readonly Dictionary<UIType, GameObject> _panels = new();
    private readonly List<UIType> _stack = new();

    public event Action<UIType> OnPanelOpened;
    public event Action<UIType> OnPanelClosed;

    public bool IsAnyPanelOpen => _stack.Count > 0;

    private WarningPopupUI _warningPopup;
    private NoticePopupUI  _noticePopup;

    // 모달 패널 뒤에 깔리는 공용 딤머 (런타임 생성)
    private GameObject _dimmer;
    private UnityEngine.UI.Image _dimmerImage;

    private Action _warningConfirmAction;
    private Action _warningCancelAction;

    protected override void Awake()
    {
        base.Awake();
        if (_instance != this) return;

        SceneManager.sceneLoaded += OnSceneLoaded;
        RegisterScenePanels();
    }

    protected override void OnDestroy()
    {
        if (_instance == this)
            SceneManager.sceneLoaded -= OnSceneLoaded;
        base.OnDestroy();
    }

    private void OnEnable()
    {
        if (_instance != this) return;

        // 인스펙터에서 바인딩이 비어 있어도(기존 인스턴스 등) 기본값으로 동작하게 보정한다.
        if (_cancelAction.bindings.Count == 0)
            _cancelAction.AddBinding("<Keyboard>/escape");

        _cancelAction.performed += OnCancelPerformed;
        _cancelAction.Enable();
    }

    private void OnDisable()
    {
        if (_instance != this) return;

        _cancelAction.performed -= OnCancelPerformed;
        _cancelAction.Disable();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RegisterScenePanels();

    // Awake 시점 스캔은 매니저 생성 순서에 따라(예: Bootstrapper가 먼저 UIManager를 만드는 경우)
    // 아직 아무것도 못 찾을 수 있고, 에디터에서 씬을 직접 실행하면 첫 씬의 sceneLoaded도 놓칠 수 있다.
    // 모든 오브젝트의 Awake가 끝난 첫 프레임에 한 번 더 스캔해 등록을 보장한다.
    private void Start()
    {
        if (_instance != this) return;
        RegisterScenePanels();
    }

    // 대부분의 UI 패널은 기본 비활성 상태라 Awake가 씬 로드 시 호출되지 않는다.
    // 그래서 비활성 오브젝트까지 포함해 직접 스캔하여 등록한다 (UIBase / SceneUIRegistrar 참고).
    private void RegisterScenePanels()
    {
        foreach (var panel in FindObjectsByType<UIBase>(FindObjectsInactive.Include))
            panel.RegisterSelf();

        foreach (var registrar in FindObjectsByType<SceneUIRegistrar>(FindObjectsInactive.Include))
            registrar.RegisterSelf();
    }

    // 취소(ESC) 입력. 매 프레임 키보드를 폴링하는 대신 InputSystem 액션으로 받아
    // 리바인딩과 게임패드 바인딩 추가가 가능하도록 한다.
    private void OnCancelPerformed(InputAction.CallbackContext _)
    {
        if (_stack.Count > 0)
            HideTop();
        else
            Show(UIType.IngameMenu);
    }

    // ── Popup Registration ─────────────────────────────────────────────

    public void RegisterWarningPopup(WarningPopupUI popup)
    {
        if (_warningPopup != null)
        {
            _warningPopup.OnConfirmed -= OnWarningConfirmed;
            _warningPopup.OnCancelled -= OnWarningCancelled;
        }
        _warningPopup = popup;
        _warningPopup.OnConfirmed += OnWarningConfirmed;
        _warningPopup.OnCancelled += OnWarningCancelled;
        Register(UIType.WarningPopup, popup.gameObject);
    }

    public void UnregisterWarningPopup()
    {
        if (_warningPopup != null)
        {
            _warningPopup.OnConfirmed -= OnWarningConfirmed;
            _warningPopup.OnCancelled -= OnWarningCancelled;
            _warningPopup = null;
        }
        Unregister(UIType.WarningPopup);
    }

    public void RegisterNoticePopup(NoticePopupUI popup)
    {
        if (_noticePopup != null)
            _noticePopup.OnOk -= OnNoticeOk;
        _noticePopup = popup;
        _noticePopup.OnOk += OnNoticeOk;
        Register(UIType.NoticePopup, popup.gameObject);
    }

    public void UnregisterNoticePopup()
    {
        if (_noticePopup != null)
        {
            _noticePopup.OnOk -= OnNoticeOk;
            _noticePopup = null;
        }
        Unregister(UIType.NoticePopup);
    }

    // ── Popup API ──────────────────────────────────────────────────────

    public void ShowWarning(string title, string message, Action onConfirm, Action onCancel = null)
    {
        // 팝업이 씬에 없으면 콜백만 저장되고 아무것도 뜨지 않아 원인 파악이 어렵다.
        if (_warningPopup == null)
        {
            Debug.LogWarning($"[UIManager] WarningPopup이 등록되지 않아 '{title}' 경고를 표시할 수 없습니다.");
            return;
        }

        _warningConfirmAction = onConfirm;
        _warningCancelAction  = onCancel;
        _warningPopup.SetContent(title, message);
        Show(UIType.WarningPopup);
    }

    public void ShowNotice(string title, string message)
    {
        if (_noticePopup == null)
        {
            Debug.LogWarning($"[UIManager] NoticePopup이 등록되지 않아 '{title}' 알림을 표시할 수 없습니다.");
            return;
        }

        _noticePopup.SetContent(title, message);
        Show(UIType.NoticePopup);
    }

    private void OnWarningConfirmed()
    {
        Hide(UIType.WarningPopup);
        _warningConfirmAction?.Invoke();
        _warningConfirmAction = null;
        _warningCancelAction  = null;
    }

    private void OnWarningCancelled()
    {
        Hide(UIType.WarningPopup);
        _warningCancelAction?.Invoke();
        _warningConfirmAction = null;
        _warningCancelAction  = null;
    }

    private void OnNoticeOk() => Hide(UIType.NoticePopup);

    // ── Panel Management ───────────────────────────────────────────────

    public void Register(UIType type, GameObject panel)
    {
        if (panel == null)
        {
            Debug.LogWarning($"[UIManager] {type} 등록에 null 패널이 전달되었습니다.");
            return;
        }

        // 같은 UIType을 서로 다른 오브젝트가 등록하면 나중 것이 이전 것을 조용히 덮어써
        // 열리지 않는 패널이 생긴다. 원인을 추적할 수 있게 경고를 남긴다.
        if (_panels.TryGetValue(type, out var existing) && existing != null && existing != panel)
            Debug.LogWarning($"[UIManager] {type} 패널이 이미 '{existing.name}'으로 등록되어 있어 '{panel.name}'으로 교체됩니다.");

        _panels[type] = panel;
    }

    public void Unregister(UIType type)
    {
        if (!_panels.Remove(type)) return;
        _stack.Remove(type);
    }

    // 같은 UIType을 다른 오브젝트가 덮어쓴 뒤 먼저 파괴되는 경우, 현재 살아있는 등록을
    // 지워버리지 않도록 소유자가 일치할 때만 해제한다.
    public void Unregister(UIType type, GameObject owner)
    {
        if (!_panels.TryGetValue(type, out var panel) || panel != owner) return;
        Unregister(type);
    }

    // 씬에 자기등록된 UI 패널을 이름 검색 없이 조회 (SceneUIRegistrar 참고)
    public GameObject GetPanel(UIType type) =>
        _panels.TryGetValue(type, out var panel) ? panel : null;

    public void Show(UIType type)
    {
        if (!_panels.TryGetValue(type, out var panel) || panel == null) return;

        // 닫힘 연출 중인 패널은 아직 active라 그냥 return하면 다시 열 수 없다. 연출을 취소하고 이어서 연다.
        bool wasClosing = _closing.Remove(panel);
        if (panel.activeSelf && !wasClosing) return;

        // 닫히는 동안 껐던 클릭 수신을 되돌린다. 놓치면 다시 열린 패널이 클릭을 못 받는다.
        if (wasClosing && panel.TryGetComponent<CanvasGroup>(out var closingGroup))
            closingGroup.blocksRaycasts = true;

        // 활성화보다 스택 등록을 먼저 한다. 씬에 비활성으로 배치된 패널은 SetActive(true) 시점에
        // Awake가 처음 호출되는데, UIBase가 "열림 스택에 있는가"로 초기 비활성 처리를 건너뛴다.
        _stack.Remove(type);
        _stack.Add(type);

        panel.SetActive(true);
        if (panel.TryGetComponent<UIBase>(out var ui)) ui.NotifyShown();

        RefreshPanelOrder();
        PlayOpenAnimation(panel);
        OnPanelOpened?.Invoke(type);
        RefreshCursor();
    }

    // 연출 도중 닫히면 알파/스케일이 중간값으로 남는다. 다음에 열 때 정상으로 보이도록 되돌린다.
    private void RestorePanelVisual(GameObject panel)
    {
        if (panel.TryGetComponent<CanvasGroup>(out var group))
        {
            DG.Tweening.DOTween.Kill(group);
            group.alpha = 1f;
            group.blocksRaycasts = true;
        }

        if (panel.transform is RectTransform rt)
        {
            DG.Tweening.DOTween.Kill(rt);
            if (_panelBaseScales.TryGetValue(rt, out var baseScale)) rt.localScale = baseScale;
        }
    }

    // 패널이 즉시 나타나면 어디가 바뀐지 알기 어렵다. 열릴 때만 짧은 페이드 + 살짝 커지는 연출을 준다.
    // 닫기는 즉시 처리한다(SetActive(false) 이후 트윈을 돌릴 수 없고, 닫힘은 즉각 반응이 더 낫다).
    private void PlayOpenAnimation(GameObject panel)
    {
        if (_openAnimationDuration <= 0f) return;

        if (!panel.TryGetComponent<CanvasGroup>(out var group))
            group = panel.AddComponent<CanvasGroup>();

        var rt = panel.transform as RectTransform;

        DG.Tweening.DOTween.Kill(group);
        if (rt != null) DG.Tweening.DOTween.Kill(rt);

        group.alpha = 0f;
        group.DOFade(1f, _openAnimationDuration)
             .SetEase(DG.Tweening.Ease.OutQuad)
             .SetUpdate(true);   // 일시정지(timeScale 0) 중에도 재생

        if (rt == null) return;

        Vector3 target = _panelBaseScales.TryGetValue(rt, out var saved) ? saved : rt.localScale;
        _panelBaseScales[rt] = target;

        rt.localScale = target * _openAnimationScaleFrom;
        rt.DOScale(target, _openAnimationDuration)
          .SetEase(DG.Tweening.Ease.OutBack)
          .SetUpdate(true);
    }

    public void Hide(UIType type)
    {
        if (!_panels.TryGetValue(type, out var panel) || panel == null || !panel.activeSelf) return;
        if (_closing.Contains(panel)) return;   // 이미 닫히는 중

        _stack.Remove(type);

        if (panel.TryGetComponent<UIBase>(out var ui)) ui.NotifyHidden();

        // 닫힘도 즉시 사라지면 딸깍거린다. 짧게 페이드아웃하되 상태(스택/이벤트/커서)는 즉시 갱신한다.
        if (_closeAnimationDuration > 0f && isActiveAndEnabled)
            PlayCloseAnimation(panel);
        else
        {
            RestorePanelVisual(panel);
            panel.SetActive(false);
        }

        RefreshPanelOrder();
        OnPanelClosed?.Invoke(type);
        RefreshCursor();
    }

    private void PlayCloseAnimation(GameObject panel)
    {
        if (!panel.TryGetComponent<CanvasGroup>(out var group))
            group = panel.AddComponent<CanvasGroup>();

        var rt = panel.transform as RectTransform;
        DG.Tweening.DOTween.Kill(group);
        if (rt != null) DG.Tweening.DOTween.Kill(rt);

        _closing.Add(panel);
        group.blocksRaycasts = false;   // 사라지는 동안 클릭을 먹지 않게

        group.DOFade(0f, _closeAnimationDuration)
             .SetEase(DG.Tweening.Ease.InQuad)
             .SetUpdate(true)
             .OnComplete(() =>
             {
                 if (!_closing.Remove(panel)) return;   // 도중에 다시 열렸다
                 group.blocksRaycasts = true;
                 RestorePanelVisual(panel);
                 panel.SetActive(false);
             });
    }

    // 그리기 순서를 씬 하이어라키 순서가 아니라 "열린 순서"로 맞춘다.
    // 자체 Canvas가 있으면 sortingOrder로, 없으면 형제 순서로 올린다.
    private void RefreshPanelOrder()
    {
        for (int i = 0; i < _stack.Count; i++)
        {
            if (!_panels.TryGetValue(_stack[i], out var panel) || panel == null) continue;

            if (panel.TryGetComponent<Canvas>(out var canvas))
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = _panelSortingOrderBase + i;
            }
            else
            {
                panel.transform.SetAsLastSibling();
            }
        }

        RefreshDimmer();
    }

    // 패널마다 딤머를 따로 두면 있는 패널과 없는 패널이 섞인다.
    // 공용 딤머 하나를 UIManager가 관리해, 필요한 패널 바로 뒤에 깔고 뒤쪽 클릭을 막는다.
    private void RefreshDimmer()
    {
        GameObject target = null;
        for (int i = _stack.Count - 1; i >= 0; i--)
        {
            if (!_panels.TryGetValue(_stack[i], out var panel) || panel == null) continue;
            if (!NeedsSharedDimmer(panel)) continue;
            target = panel;
            break;
        }

        if (target == null || target.transform.parent == null)
        {
            if (_dimmer != null) _dimmer.SetActive(false);
            return;
        }

        EnsureDimmer();
        _dimmer.transform.SetParent(target.transform.parent, false);

        var rt = (RectTransform)_dimmer.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        _dimmer.SetActive(true);
        // 딤머가 기존에 대상보다 앞에 있던 경우 같은 인덱스로 이동하면 오히려 대상 위로 올라간다.
        // 배치 후 대상 패널을 딤머 바로 다음 형제로 강제해 렌더링과 raycast 순서를 보장한다.
        _dimmer.transform.SetSiblingIndex(target.transform.GetSiblingIndex());
        target.transform.SetSiblingIndex(_dimmer.transform.GetSiblingIndex() + 1);
    }

    private static bool NeedsSharedDimmer(GameObject panel)
    {
        if (panel.TryGetComponent<UIBase>(out var ui)) return ui.UseSharedDimmer;
        if (panel.TryGetComponent<SceneUIRegistrar>(out var reg)) return reg.UseSharedDimmer;
        return false;
    }

    private void EnsureDimmer()
    {
        if (_dimmer != null) return;

        // 구조: SharedDimmer ─ BlurBackdrop(블러, 자동 생성) ─ Dim(딤 색)
        // UI는 부모→자식 순으로 그려지므로 Dim을 자식으로 두어야 블러 위에 얹힌다.
        // 블러 컴포넌트는 활성화되는 순간 BlurBackdrop을 첫 자식으로 끼워넣으므로,
        // 배치가 끝나기 전에 켜지지 않도록 꺼둔 채로 조립한다.
        _dimmer = new GameObject("SharedDimmer", typeof(RectTransform));
        _dimmer.SetActive(false);

        UIRealtimeBackdropBlur.Attach(_dimmer);

        var dimObject = new GameObject("Dim", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        dimObject.layer = _dimmer.layer;
        dimObject.transform.SetParent(_dimmer.transform, false);

        var dimRect = (RectTransform)dimObject.transform;
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;

        var theme = UITheme.Default;
        _dimmerImage = dimObject.GetComponent<UnityEngine.UI.Image>();
        _dimmerImage.raycastTarget = true;   // 뒤쪽 UI/월드 클릭 차단
        _dimmerImage.color = theme != null ? theme.Dimmer : new Color(0f, 0f, 0f, 0.6f);
    }

    public void Toggle(UIType type)
    {
        if (_panels.TryGetValue(type, out var panel) && panel != null && panel.activeSelf)
            Hide(type);
        else
            Show(type);
    }

    public void HideTop()
    {
        if (_stack.Count == 0) return;
        Hide(_stack[_stack.Count - 1]);
    }

    public void HideAll()
    {
        // Hide()가 _stack을 수정하므로 스냅샷을 떠서 위에서부터 닫는다.
        var open = _stack.ToArray();
        for (int i = open.Length - 1; i >= 0; i--)
            Hide(open[i]);

        // 닫히지 못한(파괴된) 항목이 남아 스택이 오염되는 것을 막는다.
        if (_stack.Count > 0)
        {
            _stack.Clear();
            RefreshCursor();
        }

        RefreshDimmer();
    }

    // UIBase가 초기 비활성 처리를 건너뛸지 판단하는 데 사용한다.
    public bool IsInOpenStack(UIType type) => _stack.Contains(type);

    public bool IsOpen(UIType type)
        => _panels.TryGetValue(type, out var panel) && panel != null && panel.activeSelf;

    public void ChangeMouseCursor(bool isCrosshair)
    {
        // ?.는 Unity의 파괴된 오브젝트를 걸러내지 못하므로 == null로 검사해야 한다
        if (CrosshairUI.Instance == null) return;
        CrosshairUI.Instance.SetGameMode(isCrosshair);
    }

    private void RefreshCursor()
    {
        if (CrosshairUI.Instance == null) return;
        CrosshairUI.Instance.SetGameMode(!IsAnyPanelOpen);
    }
}
