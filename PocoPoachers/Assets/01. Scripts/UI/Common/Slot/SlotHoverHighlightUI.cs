using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 아이템 슬롯에 마우스를 올렸을 때 테두리를 밝히고 살짝 키운다.
// 슬롯이 많은 화면에서 "지금 어느 칸을 가리키는지"를 알려주는 최소한의 피드백이다.
[DisallowMultipleComponent]
public class SlotHoverHighlightUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("비워두면 자식에서 테두리(SlotFrame/Frame/outline)를 찾아 쓴다.")]
    [SerializeField] private Graphic _border;

    [Tooltip("비워두면 Resources의 UITheme을 사용한다.")]
    [SerializeField] private UITheme _theme;

    private static readonly string[] BorderNames = { "SlotFrame", "Frame", "outline", "Outline", "Border" };

    private RectTransform _rect;
    private Vector3 _baseScale = Vector3.one;
    private Color _baseColor = Color.white;
    private bool _cached;

    private UITheme Theme => _theme != null ? _theme : UITheme.Default;

    private void OnDisable()
    {
        if (_rect != null) DOTween.Kill(_rect);
        if (_border != null) DOTween.Kill(_border);
        Restore();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        Cache();
        var theme = Theme;
        if (theme == null) return;

        if (_border != null)
        {
            DOTween.Kill(_border);
            _border.DOColor(theme.SlotHoverBorder, theme.ButtonMotionDuration).SetUpdate(true);
        }

        if (_rect != null)
        {
            DOTween.Kill(_rect);
            _rect.DOScale(_baseScale * theme.SlotHoverScale, theme.ButtonMotionDuration)
                 .SetEase(Ease.OutQuad).SetUpdate(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Cache();
        var theme = Theme;
        float duration = theme != null ? theme.ButtonMotionDuration : 0.08f;

        if (_border != null)
        {
            DOTween.Kill(_border);
            _border.DOColor(_baseColor, duration).SetUpdate(true);
        }

        if (_rect != null)
        {
            DOTween.Kill(_rect);
            _rect.DOScale(_baseScale, duration).SetEase(Ease.OutQuad).SetUpdate(true);
        }
    }

    private void Cache()
    {
        if (_cached) return;

        _rect = transform as RectTransform;
        if (_rect != null) _baseScale = _rect.localScale;

        if (_border == null) _border = FindBorder();
        if (_border != null) _baseColor = _border.color;

        _cached = true;
    }

    private void Restore()
    {
        if (!_cached) return;
        if (_rect != null) _rect.localScale = _baseScale;
        if (_border != null) _border.color = _baseColor;
    }

    // 테두리를 별도 자식으로 둔 슬롯(InventorySlotUI/SlotFrame 등)은 그 자식을 쓰고,
    // 루트 Image 자체가 슬롯 프레임인 슬롯(WeaponSlotUI/GunPartSlot 등)은 자기 그래픽을 쓴다.
    private Graphic FindBorder()
    {
        foreach (var g in GetComponentsInChildren<Graphic>(true))
        {
            if (g.gameObject == gameObject) continue;
            for (int i = 0; i < BorderNames.Length; i++)
                if (g.name == BorderNames[i]) return g;
        }
        return GetComponent<Graphic>();
    }
}
