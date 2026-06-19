using UnityEngine;
using UnityEngine.UI;

public abstract class ProgressUIBase : MonoBehaviour
{
    [SerializeField] protected Slider _slider;

    private float _duration;
    private float _elapsed;
    private bool _isFilling;

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

    protected virtual void Update()
    {
        if (!_isFilling) return;

        _elapsed += Time.deltaTime;
        _slider.value = Mathf.Clamp01(_elapsed / _duration);

        if (_elapsed >= _duration)
            StopFilling();
    }

    protected void Show() => gameObject.SetActive(true);
    protected void Hide() => gameObject.SetActive(false);

    protected void StartFilling(float duration)
    {
        _duration = duration;
        _elapsed = 0f;
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
