using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Slider 값을 목표치로 부드럽게 보간하는 헬퍼 (HpUI, HpWorldUI 등에서 재사용)
public class AnimatedSliderFill
{
    private readonly Slider _slider;
    private readonly float _speed;

    public AnimatedSliderFill(Slider slider, float speed)
    {
        _slider = slider;
        _speed = speed;
    }

    public IEnumerator AnimateTo(float target)
    {
        while (!Mathf.Approximately(_slider.value, target))
        {
            _slider.value = Mathf.MoveTowards(_slider.value, target, _speed * Time.deltaTime);
            yield return null;
        }
        _slider.value = target;
    }
}
