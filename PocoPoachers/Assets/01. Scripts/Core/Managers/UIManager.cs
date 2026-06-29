using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

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
        _panels[type] = panel;
    }

    public void Unregister(UIType type)
    {
        if (!_panels.Remove(type)) return;
        _stack.Remove(type);
    }

    public void Show(UIType type)
    {
        if (!_panels.TryGetValue(type, out var panel) || panel == null || panel.activeSelf) return;

        panel.SetActive(true);
        _stack.Add(type);
        OnPanelOpened?.Invoke(type);
        RefreshCursor();
    }

    public void Hide(UIType type)
    {
        if (!_panels.TryGetValue(type, out var panel) || panel == null || !panel.activeSelf) return;

        panel.SetActive(false);
        _stack.Remove(type);
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
        for (int i = _stack.Count - 1; i >= 0; i--)
        {
            if (_panels.TryGetValue(_stack[i], out var panel) && panel != null)
                panel.SetActive(false);
            OnPanelClosed?.Invoke(_stack[i]);
        }
        _stack.Clear();
        RefreshCursor();
    }

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
