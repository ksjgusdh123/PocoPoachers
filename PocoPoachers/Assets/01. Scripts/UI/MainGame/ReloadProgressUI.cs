using UnityEngine;
using UnityEngine.UI;

public class ReloadProgressUI : MonoBehaviour
{
    [SerializeField] private Slider _slider;

    private float _duration;
    private float _elapsed;
    private bool _isFilling;

    private void Awake()
    {
        GunBase.OnReloadStarted += StartFilling;
        GunBase.OnReloadEnded += StopFilling;
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        GunBase.OnReloadStarted -= StartFilling;
        GunBase.OnReloadEnded -= StopFilling;
    }

    private void Update()
    {
        if (!_isFilling) return;

        _elapsed += Time.deltaTime;
        _slider.value = Mathf.Clamp01(_elapsed / _duration);

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
