using TMPro;
using UnityEngine;

// TMP 텍스트의 크기와 Auto Size 범위를 UITheme의 타이포 역할에 맞춘다.
// 색상은 아이템 등급이나 경고처럼 기능적인 의미가 있으므로 이 컴포넌트가 덮어쓰지 않는다.
[RequireComponent(typeof(TMP_Text))]
[DisallowMultipleComponent]
public class ThemedTextUI : MonoBehaviour
{
    [SerializeField] private UITheme.TypographyRole _role = UITheme.TypographyRole.Body;

    [Tooltip("Auto Size가 켜진 텍스트의 최소/최대 크기도 역할 토큰에 맞춘다.")]
    [SerializeField] private bool _manageAutoSizeRange = true;

    [Tooltip("비워두면 Resources의 UITheme을 사용한다.")]
    [SerializeField] private UITheme _theme;

    private TMP_Text _text;

    public UITheme.TypographyRole Role => _role;

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

    public void Apply()
    {
        if (_text == null) _text = GetComponent<TMP_Text>();

        UITheme theme = Theme;
        if (_text == null || theme == null) return;

        _text.fontSize = theme.GetFontSize(_role);
        if (!_text.enableAutoSizing || !_manageAutoSizeRange) return;

        Vector2 range = theme.GetAutoSizeRange(_role);
        _text.fontSizeMin = range.x;
        _text.fontSizeMax = range.y;
    }
}
