using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 버튼의 상호작용 색상을 UITheme에서 가져와 적용하고, 호버/누름에 짧은 스케일 모션을 준다.
// 기존에는 모든 버튼이 Unity 기본 ColorBlock(흰색 -> #F5F5F5)이라 호버 피드백이 사실상 없었다.
[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class ThemedButtonUI : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private UITheme.ButtonStyle _style = UITheme.ButtonStyle.Primary;

    [Tooltip("비워두면 Resources의 UITheme을 사용한다.")]
    [SerializeField] private UITheme _theme;

    [Tooltip("호버/누름 시 스케일 모션을 사용한다.")]
    [SerializeField] private bool _useMotion = true;

    private Button _button;
    private RectTransform _rect;
    private Vector3 _baseScale = Vector3.one;
    private bool _baseScaleCached;
    private bool _hovered;

    private UITheme Theme => _theme != null ? _theme : UITheme.Default;

    private void Awake() => Apply();

    private void OnEnable()
    {
        Apply();
        ResetScale();
    }

    private void OnDisable()
    {
        _hovered = false;
        if (_rect != null) DOTween.Kill(_rect);
        ResetScale();
    }

#if UNITY_EDITOR
    private void OnValidate() => Apply();
#endif

    public void Apply()
    {
        if (_button == null) _button = GetComponent<Button>();
        if (_button == null) return;

        UITheme theme = Theme;
        if (theme == null) return;

        _button.transition = Selectable.Transition.ColorTint;
        _button.colors = theme.GetColorBlock(_style);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _hovered = true;
        if (!CanAnimate()) return;
        ScaleTo(Theme.ButtonHoverScale);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _hovered = false;
        if (!CanAnimate()) return;
        ScaleTo(1f);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!CanAnimate()) return;
        ScaleTo(Theme.ButtonPressScale);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!CanAnimate()) return;
        ScaleTo(_hovered ? Theme.ButtonHoverScale : 1f);
    }

    private bool CanAnimate()
    {
        if (!_useMotion || Theme == null) return false;
        if (_button == null) _button = GetComponent<Button>();
        return _button != null && _button.interactable && isActiveAndEnabled;
    }

    private void ScaleTo(float multiplier)
    {
        CacheRect();
        if (_rect == null) return;

        DOTween.Kill(_rect);
        _rect.DOScale(_baseScale * multiplier, Theme.ButtonMotionDuration)
             .SetEase(Ease.OutQuad)
             .SetUpdate(true);   // 일시정지 중에도 반응하도록
    }

    private void ResetScale()
    {
        CacheRect();
        if (_rect != null && _baseScaleCached) _rect.localScale = _baseScale;
    }

    private void CacheRect()
    {
        if (_rect == null) _rect = transform as RectTransform;
        if (_rect == null || _baseScaleCached) return;

        _baseScale = _rect.localScale;
        _baseScaleCached = true;
    }
}
