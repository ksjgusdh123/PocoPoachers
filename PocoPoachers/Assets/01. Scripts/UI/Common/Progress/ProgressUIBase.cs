using UnityEngine;
using UnityEngine.UI;

public abstract class ProgressUIBase : MonoBehaviour
{
    [SerializeField] protected Slider _slider;

    private float _duration;
    private float _elapsed;
    private bool _isFilling;

    protected virtual void Update()
    {
        if (!_isFilling) return;

        _elapsed += Time.deltaTime;
        _slider.value = Mathf.Clamp01(_elapsed / _duration);

        if (_elapsed >= _duration)
            StopFilling();
    }

    protected void StartFilling(float duration)
    {
        _duration = duration;
        _elapsed = 0f;
        _slider.value = 0f;
        _isFilling = true;
        gameObject.SetActive(true);
    }

    protected void StopFilling()
    {
        _isFilling = false;
        _slider.value = 0f;
        gameObject.SetActive(false);
    }
}
