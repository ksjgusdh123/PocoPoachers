using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HpWorldUI : WorldUIBase
{
    [SerializeField] private Slider _slider;
    [SerializeField] private float _animSpeed = 5f;
    [SerializeField] private float _hideDelay = 3f;

    private StatBase _stat;
    private float _targetValue;
    private Coroutine _animCoroutine;
    private Coroutine _hideCoroutine;
    private AnimatedSliderFill _sliderFill;

    private static readonly Dictionary<StatBase, HpWorldUI> _active = new Dictionary<StatBase, HpWorldUI>();

    public static void Hide(StatBase stat)
    {
        if (_active.TryGetValue(stat, out var ui))
            ui.Release();
    }

    public static void Show(StatBase stat)
    {
        // 이미 표시 중이면 숨김 타이머만 리셋
        if (_active.TryGetValue(stat, out var existing))
        {
            existing.ResetHideTimer();
            return;
        }

        var ui = WorldUIManager.Instance.Create<HpWorldUI>(WorldUIType.HpBar, stat.transform);
        if (ui == null) return;

        ui.SetStat(stat);
        _active[stat] = ui;
    }

    private void SetStat(StatBase stat)
    {
        _stat = stat;
        _sliderFill = new AnimatedSliderFill(_slider, _animSpeed);

        _targetValue = stat.MaxHp > 0f ? stat.CurrentHp / stat.MaxHp : 0f;
        _slider.value = _targetValue;

        stat.OnHpChanged += Refresh;
        stat.OnDie += OnDie;

        ResetHideTimer();
    }

    public void ResetHideTimer()
    {
        if (_hideCoroutine != null)
            StopCoroutine(_hideCoroutine);
        _hideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private void OnDisable()
    {
        if (_stat != null)
        {
            _active.Remove(_stat);
            _stat.OnHpChanged -= Refresh;
            _stat.OnDie -= OnDie;
            _stat = null;
        }

        if (_animCoroutine != null) { StopCoroutine(_animCoroutine); _animCoroutine = null; }
        if (_hideCoroutine != null) { StopCoroutine(_hideCoroutine); _hideCoroutine = null; }
    }

    private void Refresh(float current, float max)
    {
        _targetValue = max > 0f ? current / max : 0f;

        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(_sliderFill.AnimateTo(_targetValue));
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(_hideDelay);
        _hideCoroutine = null;
        Release();
    }

    private void OnDie()
    {
        Release();
    }
}
