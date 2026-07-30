using UnityEngine;
using UnityEngine.UI;

// Slider, Dropdown, InputField, Toggle 등 버튼 외 기본 위젯의 상태 색을 통일한다.
// 버튼은 모션까지 담당하는 ThemedButtonUI를 사용한다.
[RequireComponent(typeof(Selectable))]
[DisallowMultipleComponent]
public class ThemedSelectableUI : MonoBehaviour
{
    [Tooltip("비워두면 Resources의 UITheme을 사용한다.")]
    [SerializeField] private UITheme _theme;

    private Selectable _selectable;

    private UITheme Theme => _theme != null ? _theme : UITheme.Default;

    private void Awake() => Apply();

    private void OnEnable() => Apply();

#if UNITY_EDITOR
    private void OnValidate() => Apply();
#endif

    public void Apply()
    {
        if (_selectable == null) _selectable = GetComponent<Selectable>();

        UITheme theme = Theme;
        if (_selectable == null || theme == null) return;

        _selectable.transition = Selectable.Transition.ColorTint;
        _selectable.colors = theme.GetSelectableColorBlock();
    }
}
