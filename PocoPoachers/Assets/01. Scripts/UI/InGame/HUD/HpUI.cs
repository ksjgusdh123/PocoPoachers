using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HpUI : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private TextMeshProUGUI _hpText;
    [SerializeField] private float _animSpeed = 5f;
    [SerializeField] private StatBase _stat;

    private float _targetValue;
    private Coroutine _animCoroutine;
    private AnimatedSliderFill _sliderFill;

    public void Setup(StatBase stat)
    {
        _stat = stat;
        _stat.OnHpChanged += Refresh;
        _sliderFill = new AnimatedSliderFill(_slider, _animSpeed);

        // 초기값은 애니메이션 없이 바로 적용
        _targetValue = _stat.MaxHp > 0f ? _stat.CurrentHp / _stat.MaxHp : 0f;
        _slider.value = _targetValue;
        UpdateText(_stat.CurrentHp, _stat.MaxHp);
    }

    private void OnDestroy()
    {
        if (_stat != null)
            _stat.OnHpChanged -= Refresh;
    }

    private void Refresh(float current, float max)
    {
        _targetValue = max > 0f ? current / max : 0f;
        UpdateText(current, max);

        if (!gameObject.activeInHierarchy)
        {
            _slider.value = _targetValue;
            return;
        }

        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(_sliderFill.AnimateTo(_targetValue));
    }

    private void UpdateText(float current, float max)
    {
        if (_hpText != null)
            _hpText.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }
}
