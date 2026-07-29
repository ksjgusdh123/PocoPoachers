using UnityEngine;
using UnityEngine.UI;

// 그래픽(Image/Text)의 색을 UITheme의 역할 토큰에서 가져와 적용한다.
// 프리팹마다 하드코딩돼 있던 색을 역할로 바꿔, 팔레트 변경을 테마 한 곳에서 처리할 수 있게 한다.
[RequireComponent(typeof(Graphic))]
[DisallowMultipleComponent]
public class ThemedGraphicUI : MonoBehaviour
{
    public enum GraphicRole
    {
        Accent,
        Surface,
        SlotSurface,
        ProgressFill,
        HealthFill,
        StaminaFill,
        Danger,
    }

    [SerializeField] private GraphicRole _role = GraphicRole.Accent;

    [Tooltip("역할 색의 알파를 무시하고 원래 그래픽의 알파를 유지한다.")]
    [SerializeField] private bool _keepAlpha = true;

    [Tooltip("비워두면 Resources의 UITheme을 사용한다.")]
    [SerializeField] private UITheme _theme;

    private Graphic _graphic;

    private void Awake() => Apply();

    private void OnEnable() => Apply();

#if UNITY_EDITOR
    private void OnValidate() => Apply();
#endif

    public void Apply()
    {
        if (_graphic == null) _graphic = GetComponent<Graphic>();
        if (_graphic == null) return;

        UITheme theme = _theme != null ? _theme : UITheme.Default;
        if (theme == null) return;

        Color color = GetRoleColor(theme);
        if (_keepAlpha) color.a = _graphic.color.a;
        _graphic.color = color;
    }

    private Color GetRoleColor(UITheme theme)
    {
        switch (_role)
        {
            case GraphicRole.Surface:      return theme.Surface;
            case GraphicRole.SlotSurface:  return theme.SlotSurface;
            case GraphicRole.ProgressFill: return theme.ProgressFill;
            case GraphicRole.HealthFill:   return theme.HealthFill;
            case GraphicRole.StaminaFill:  return theme.StaminaFill;
            case GraphicRole.Danger:       return theme.Danger;
            default:                       return theme.Accent;
        }
    }
}
