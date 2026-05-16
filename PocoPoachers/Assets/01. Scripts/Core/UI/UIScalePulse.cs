using UnityEngine;

public class UIScalePulse : WorldUIElement
{
    [SerializeField] private RectTransform _targetTransform;

    [Header("스케일 설정")]
    [SerializeField] private float _minScale = 0.9f;
    [SerializeField] private float _maxScale = 1.1f;
    [SerializeField] private float _speed = 2f;

    [Header("재생 설정")]
    [SerializeField] private bool _playOnAwake = true;

    private Vector3 _originalScale;
    private float _time;
    private bool _isPlaying;

    private void Awake()
    {
        _originalScale = _targetTransform.localScale;
    }

    private void OnEnable()
    {
        _time = 0f;
        if (_playOnAwake)
            Play();
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _time += Time.deltaTime * _speed;
        float scale = Mathf.Lerp(_minScale, _maxScale, (Mathf.Sin(_time) + 1f) * 0.5f);
        _targetTransform.localScale = _originalScale * scale;
    }

    public void Play()
    {
        _isPlaying = true;
    }

    public void Stop()
    {
        _isPlaying = false;
        _targetTransform.localScale = _originalScale;
        _time = 0f;
    }

    public void Pause()
    {
        _isPlaying = false;
    }
}
