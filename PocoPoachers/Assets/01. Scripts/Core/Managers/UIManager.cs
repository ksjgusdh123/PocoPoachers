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
}

public class UIManager : Singleton<UIManager>
{
    private readonly Dictionary<UIType, GameObject> _panels = new();
    private readonly List<UIType> _stack = new();

    public event Action<UIType> OnPanelOpened;
    public event Action<UIType> OnPanelClosed;

    public bool IsAnyPanelOpen => _stack.Count > 0;

    private WarningPopupUI _warningPopup;
    private NoticePopupUI  _noticePopup;

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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RegisterScenePanels();

    // 대부분의 UI 패널은 기본 비활성 상태라 Awake가 씬 로드 시 호출되지 않는다.
    // 그래서 비활성 오브젝트까지 포함해 직접 스캔하여 등록한다 (UIBase / SceneUIRegistrar 참고).
    private void RegisterScenePanels()
    {
        foreach (var panel in FindObjectsByType<UIBase>(FindObjectsInactive.Include))
            panel.RegisterSelf();

        foreach (var registrar in FindObjectsByType<SceneUIRegistrar>(FindObjectsInactive.Include))
            registrar.RegisterSelf();
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

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
        _warningConfirmAction = onConfirm;
        _warningCancelAction  = onCancel;
        _warningPopup?.SetContent(title, message);
        Show(UIType.WarningPopup);
    }

    public void ShowNotice(string title, string message)
    {
        _noticePopup?.SetContent(title, message);
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
        if (!_panels.TryGetValue(type, out var panel) || panel == null || panel.activeSelf) return;

        // 활성화보다 스택 등록을 먼저 한다. 씬에 비활성으로 배치된 패널은 SetActive(true) 시점에
        // Awake가 처음 호출되는데, UIBase가 "열림 스택에 있는가"로 초기 비활성 처리를 건너뛴다.
        _stack.Remove(type);
        _stack.Add(type);

        panel.SetActive(true);
        if (panel.TryGetComponent<UIBase>(out var ui)) ui.NotifyShown();

        OnPanelOpened?.Invoke(type);
        RefreshCursor();
    }

    public void Hide(UIType type)
    {
        if (!_panels.TryGetValue(type, out var panel) || panel == null || !panel.activeSelf) return;

        _stack.Remove(type);

        if (panel.TryGetComponent<UIBase>(out var ui)) ui.NotifyHidden();
        panel.SetActive(false);

        OnPanelClosed?.Invoke(type);
        RefreshCursor();
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
    }

    // UIBase가 초기 비활성 처리를 건너뛸지 판단하는 데 사용한다.
    public bool IsInOpenStack(UIType type) => _stack.Contains(type);

    public bool IsOpen(UIType type)
        => _panels.TryGetValue(type, out var panel) && panel != null && panel.activeSelf;

    public void ChangeMouseCursor(bool isCrosshair)
    {
        CrosshairUI.Instance?.SetGameMode(isCrosshair);
    }

    private void RefreshCursor()
    {
        if (CrosshairUI.Instance == null) return;
        CrosshairUI.Instance.SetGameMode(!IsAnyPanelOpen);
    }
}
