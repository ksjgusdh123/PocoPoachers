using System;
using UnityEngine;
using UnityEngine.EventSystems;

// 미니맵 확대/축소(휠) + 이동(휠 클릭 드래그).
// 이 컴포넌트는 잘라내기용 뷰포트(RectMask2D가 붙은 고정 크기 오브젝트)에 붙인다.
// _mapContent엔 실제로 커지고 움직이는 지도(MinimapImage의 RectTransform)를 연결 —
// 마커/핑이 전부 그 자식이라 같이 확대/이동된다.
//
// 슬라이더 등 다른 UI에서 조절하려면 NormalizedZoom을 쓰고, 휠로 바뀐 값을 따라가려면
// ZoomChanged를 구독한다 (MinimapZoomSlider 참고).
public class MinimapZoomPan : MonoBehaviour, IScrollHandler, IDragHandler
{
    [SerializeField] private RectTransform _mapContent;
    [SerializeField] private float _zoomStep = 0.1f;
    [SerializeField] private float _maxZoom = 3f;

    private float _minZoom; // 시작 시점의 스케일 — 이보다 더 축소되지 않는다
    private Vector2 _initialPosition;

    // 배율이 바뀔 때마다 0~1로 정규화한 값을 알린다. 휠로 바꿔도 슬라이더가 따라오게 하기 위한 것.
    public event Action<float> ZoomChanged;

    public float NormalizedZoom
    {
        get => _mapContent == null ? 0f : Mathf.InverseLerp(_minZoom, _maxZoom, _mapContent.localScale.x);
        set => ApplyZoom(Mathf.Lerp(_minZoom, _maxZoom, Mathf.Clamp01(value)));
    }

    private void Awake()
    {
        if (_mapContent == null) return;
        _minZoom = _mapContent.localScale.x;
        _initialPosition = _mapContent.anchoredPosition;
    }

    // 미니맵을 열 때마다 호출 — 이전에 확대/이동했던 걸 원래 크기·위치로 되돌린다
    public void ResetView()
    {
        if (_mapContent == null) return;

        _mapContent.anchoredPosition = _initialPosition;
        ApplyZoom(_minZoom);
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (_mapContent == null) return;

        ApplyZoom(_mapContent.localScale.x + eventData.scrollDelta.y * _zoomStep);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (_mapContent == null) return;
        if (eventData.button != PointerEventData.InputButton.Middle) return;

        _mapContent.anchoredPosition += eventData.delta;
        ClampContent();
    }

    private void ApplyZoom(float scale)
    {
        scale = Mathf.Clamp(scale, _minZoom, _maxZoom);
        _mapContent.localScale = new Vector3(scale, scale, 1f);

        ClampContent(); // 줄어든 스케일 때문에 기존 위치가 범위 밖으로 나갈 수 있어 다시 잡아준다
        ZoomChanged?.Invoke(NormalizedZoom);
    }

    // 지도가 뷰포트보다 큰 만큼만 이동 가능하게 막는다 — 빈 공간이 보이지 않도록
    private void ClampContent()
    {
        var viewport = (RectTransform)transform;

        Vector2 contentSize = new Vector2(
            _mapContent.rect.width * _mapContent.localScale.x,
            _mapContent.rect.height * _mapContent.localScale.y);
        Vector2 viewportSize = viewport.rect.size;

        Vector2 maxOffset = Vector2.Max((contentSize - viewportSize) * 0.5f, Vector2.zero);

        Vector2 pos = _mapContent.anchoredPosition;
        pos.x = Mathf.Clamp(pos.x, -maxOffset.x, maxOffset.x);
        pos.y = Mathf.Clamp(pos.y, -maxOffset.y, maxOffset.y);
        _mapContent.anchoredPosition = pos;
    }
}
