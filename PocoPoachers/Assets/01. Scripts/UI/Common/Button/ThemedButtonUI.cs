using UnityEngine;
using UnityEngine.UI;

// 버튼의 상호작용 색상을 UITheme에서 가져와 적용한다.
// 기존에는 모든 버튼이 Unity 기본 ColorBlock(흰색 → #F5F5F5)을 쓰고 있어서
// 호버 시 밝기 차이가 4% 수준이라 피드백이 사실상 보이지 않았다.
[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class ThemedButtonUI : MonoBehaviour
{
    [SerializeField] private UITheme.ButtonStyle _style = UITheme.ButtonStyle.Primary;

    [Tooltip("비워두면 Resources의 UITheme을 사용한다.")]
    [SerializeField] private UITheme _theme;

    private Button _button;

    private void Awake() => Apply();

    private void OnEnable() => Apply();

#if UNITY_EDITOR
    private void OnValidate() => Apply();
#endif

    public void Apply()
    {
        if (_button == null) _button = GetComponent<Button>();
        if (_button == null) return;

        UITheme theme = _theme != null ? _theme : UITheme.Default;
        if (theme == null) return;

        _button.transition = Selectable.Transition.ColorTint;
        _button.colors = theme.GetColorBlock(_style);
    }
}
