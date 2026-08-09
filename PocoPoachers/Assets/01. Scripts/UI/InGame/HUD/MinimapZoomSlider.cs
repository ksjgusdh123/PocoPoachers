using UnityEngine;
using UnityEngine.UI;

// 미니맵 옆에 두는 확대/축소 슬라이더. 손잡이를 위아래로 끌면 배율이 바뀐다.
// 휠로 확대한 경우에도 슬라이더가 따라오도록 MinimapZoomPan.ZoomChanged를 구독한다.
//
// 붙이는 곳: Slider 컴포넌트가 있는 오브젝트 (Direction을 Bottom To Top으로 두면 위가 확대).
[RequireComponent(typeof(Slider))]
public class MinimapZoomSlider : MonoBehaviour
{
    [SerializeField] private MinimapZoomPan _zoomPan;

    private Slider _slider;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        _slider.minValue = 0f;
        _slider.maxValue = 1f;
        _slider.wholeNumbers = false;
    }

    private void OnEnable()
    {
        if (_zoomPan == null)
        {
            Debug.LogWarning($"[{nameof(MinimapZoomSlider)}] MinimapZoomPan이 연결되지 않았습니다.", this);
            return;
        }

        _slider.onValueChanged.AddListener(OnSliderChanged);
        _zoomPan.ZoomChanged += OnZoomChanged;

        _slider.SetValueWithoutNotify(_zoomPan.NormalizedZoom);
    }

    private void OnDisable()
    {
        _slider.onValueChanged.RemoveListener(OnSliderChanged);
        if (_zoomPan != null) _zoomPan.ZoomChanged -= OnZoomChanged;
    }

    private void OnSliderChanged(float value) => _zoomPan.NormalizedZoom = value;

    // 되먹임을 막으려면 반드시 알림 없이 값만 넣어야 한다.
    private void OnZoomChanged(float value) => _slider.SetValueWithoutNotify(value);
}
