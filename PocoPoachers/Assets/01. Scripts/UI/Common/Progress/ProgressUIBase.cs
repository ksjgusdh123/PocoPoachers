using UnityEngine;
using UnityEngine.UI;

public abstract class ProgressUIBase : MonoBehaviour
{
    [SerializeField] protected Slider _slider;

    private float _duration;
    private bool _isFilling;

    // 경과는 deltaTime 누적이 아니라 시작 시각으로 잰다. 이 UI는 MainGameUI 안에 있어
    // 인벤토리를 열면 통째로 비활성이 되는데, 그동안 Update가 멈춰 누적이 끊긴다.
    // 그러면 이미 끝난 사용인데도 인벤토리를 닫는 순간 게이지가 0부터 다시 찬다.
    private float _startTime;

    protected virtual void Awake()
    {
        Subscribe();
        Hide();
    }

    protected virtual void OnDestroy()
    {
        Unsubscribe();
    }

    protected abstract void Subscribe();
    protected abstract void Unsubscribe();

    // 꺼져 있는 동안 흐른 시간을 켜지는 즉시 반영한다 — 빈 게이지가 한 프레임 스치지 않도록
    protected virtual void OnEnable()
    {
        if (_isFilling) RefreshFill();
    }

    protected virtual void Update()
    {
        if (!_isFilling) return;

        RefreshFill();
    }

    private void RefreshFill()
    {
        float elapsed = Time.time - _startTime;
        _slider.value = _duration > 0f ? Mathf.Clamp01(elapsed / _duration) : 1f;

        if (elapsed >= _duration)
            StopFilling();
    }

    protected void Show() => gameObject.SetActive(true);
    protected void Hide() => gameObject.SetActive(false);

    protected void StartFilling(float duration)
    {
        _duration = duration;
        _startTime = Time.time;
        _slider.value = 0f;
        _isFilling = true;
        Show();
    }

    protected void StopFilling()
    {
        _isFilling = false;
        _slider.value = 0f;
        Hide();
    }

    // 시간 기반이 아닌, 외부에서 계산된 진행률(예: AsyncOperation.progress)을 그대로 반영할 때 사용
    protected void SetProgress(float value)
    {
        _isFilling = false;
        _slider.value = Mathf.Clamp01(value);
    }
}
