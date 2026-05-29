using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
public enum UIType
{
    Inventory,
    Storage,
    ItemBoxReveal,
    EquipContextMenu,
    IngameMenu,
    WarningPopup,
    NoticePopup,
    JoinCode,
}

public class UIManager : Singleton<UIManager>
{
    private readonly Dictionary<UIType, GameObject> _panels = new();
    private readonly List<UIType> _stack = new();

    public event Action<UIType> OnPanelOpened;
    public event Action<UIType> OnPanelClosed;

    public bool IsAnyPanelOpen => _stack.Count > 0;

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        if (_stack.Count > 0)
            HideTop();
        else
            Show(UIType.IngameMenu);
    }

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

    // 커서 모드를 직접 강제 지정할 때 사용 (씬 전환 등)
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
