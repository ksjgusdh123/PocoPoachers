using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class PopupUIBase : UIBase
{
    [SerializeField] private UIPopupTheme _theme;

    protected abstract TextMeshProUGUI TitleText { get; }
    protected abstract TextMeshProUGUI MessageText { get; }
    protected abstract Button[] Buttons { get; }

    protected override void Awake()
    {
        base.Awake();
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        if (_theme == null) return;

        if (TryGetComponent<Image>(out var panel))
        {
            if (_theme.panelSprite != null) panel.sprite = _theme.panelSprite;
            panel.color = _theme.panelColor;
        }

        UIThemeUtil.ApplyText(TitleText, _theme.titleFont, _theme.titleColor);
        UIThemeUtil.ApplyText(MessageText, _theme.messageFont, _theme.messageColor);

        foreach (var button in Buttons)
            UIThemeUtil.ApplyButtonStyle(button, _theme.buttonSprite, _theme.buttonColors, _theme.buttonFont, _theme.buttonTextColor);
    }
}
