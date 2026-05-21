using UnityEngine;
using UnityEngine.UI;

// 아이템 사용 게이지 슬라이더 UI
// PlayerController의 OnUseStarted / OnUseCancelled 이벤트를 구독해 동작한다
public class ItemUseProgressUI : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private float _duration;
    private float _elapsed;
    private bool _isFilling;

    private void Awake()
    {
        PlayerController.OnUseStarted += StartFilling;
        PlayerController.OnUseCancelled += StopFilling;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        PlayerController.OnUseStarted -= StartFilling;
        PlayerController.OnUseCancelled -= StopFilling;
    }

    private void Update()
    {
        if (!_isFilling) return;

        _elapsed += Time.deltaTime;
        _slider.value = Mathf.Clamp01(_elapsed / _duration);

        // 슬라이더가 다 차면 자동으로 숨긴다 (실제 소모는 PlayerController 코루틴이 처리)
        if (_elapsed >= _duration)
            StopFilling();
    }

    private void StartFilling(float duration)
    {
        _duration = duration;
        _elapsed = 0f;
        _slider.value = 0f;
        _isFilling = true;
        gameObject.SetActive(true);
    }

    private void StopFilling()
    {
        _isFilling = false;
        _slider.value = 0f;
        gameObject.SetActive(false);
    }
}
