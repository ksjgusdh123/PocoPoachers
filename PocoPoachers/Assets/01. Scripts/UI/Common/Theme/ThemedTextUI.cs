using TMPro;
using UnityEngine;

// TMP 텍스트의 크기와 Auto Size 범위를 UITheme의 타이포 역할에 맞춘다.
// 색은 _colorRole이 None이 아닐 때만 적용한다. 아이템 등급이나 런타임 상태 표시처럼
// 코드가 직접 색을 바꾸는 텍스트는 None으로 두면 이 컴포넌트가 관여하지 않는다.
[RequireComponent(typeof(TMP_Text))]
[DisallowMultipleComponent]
public class ThemedTextUI : MonoBehaviour
{
    [SerializeField] private UITheme.TypographyRole _role = UITheme.TypographyRole.Body;

    [Tooltip("끄면 크기는 그대로 두고 색만 관리한다. 월드 공간 캔버스처럼 스크린 타이포 스케일이 맞지 않는 텍스트에 사용.")]
    [SerializeField] private bool _manageFontSize = true;

    [Tooltip("Auto Size가 켜진 텍스트의 최소/최대 크기도 역할 토큰에 맞춘다.")]
    [SerializeField] private bool _manageAutoSizeRange = true;

    [Tooltip("None이면 색을 건드리지 않는다.")]
    [SerializeField] private UITheme.TextColorRole _colorRole = UITheme.TextColorRole.None;

    [Tooltip("색 역할을 적용할 때 원래 알파를 유지한다.")]
    [SerializeField] private bool _keepAlpha = true;

    [Tooltip("비워두면 Resources의 UITheme을 사용한다.")]
    [SerializeField] private UITheme _theme;

    private TMP_Text _text;

    // 잠긴 스킬 이름처럼 코드가 상태에 따라 색을 직접 칠하는 동안에는 테마 색 적용을 양보한다.
    // 색만 비켜줄 뿐 크기 관리는 그대로 둔다.
    private bool _colorOverridden;

    public UITheme.TypographyRole Role => _role;

    public UITheme.TextColorRole ColorRole => _colorRole;

    private UITheme Theme => _theme != null ? _theme : UITheme.Default;

    private void Awake() => Apply();

    private void OnEnable() => Apply();

#if UNITY_EDITOR
    private void OnValidate() => Apply();
#endif

    public void Configure(UITheme.TypographyRole role, bool manageAutoSizeRange = true)
    {
        _role = role;
        _manageAutoSizeRange = manageAutoSizeRange;
        Apply();
    }

    public void SetColorOverride(bool overridden)
    {
        if (_colorOverridden == overridden) return;

        _colorOverridden = overridden;
        if (!_colorOverridden) Apply();
    }

    public void ConfigureColor(UITheme.TextColorRole colorRole)
    {
        _colorRole = colorRole;
        Apply();
    }

    public void Apply()
    {
        if (_text == null) _text = GetComponent<TMP_Text>();

        UITheme theme = Theme;
        if (_text == null || theme == null) return;

        ApplyColor(theme);

        if (!_manageFontSize) return;

        _text.fontSize = theme.GetFontSize(_role);
        if (!_text.enableAutoSizing || !_manageAutoSizeRange) return;

        Vector2 range = theme.GetAutoSizeRange(_role);
        _text.fontSizeMin = range.x;
        _text.fontSizeMax = range.y;
    }

    private void ApplyColor(UITheme theme)
    {
        if (_colorOverridden) return;
        if (_colorRole == UITheme.TextColorRole.None) return;

        Color color = theme.GetTextColor(_colorRole);
        if (_keepAlpha) color.a = _text.color.a;
        _text.color = color;
    }
}
