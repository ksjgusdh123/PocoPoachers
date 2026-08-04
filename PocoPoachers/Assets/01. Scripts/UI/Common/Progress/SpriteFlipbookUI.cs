using UnityEngine;
using UnityEngine.UI;

// Sprite 배열을 순서대로 갈아끼워서 플립북 애니메이션을 재생한다 (예: 로켓 엔진의 화염 효과).
[RequireComponent(typeof(Image))]
public class SpriteFlipbookUI : MonoBehaviour
{
    [SerializeField] private Sprite[] _frames;
    [SerializeField] private float _frameRate = 24f; // 초당 프레임 수
    [SerializeField] private bool _loop = true;
    [SerializeField] private bool _playOnEnable = true;

    private Image _image;
    private float _timer;
    private int _index;
    private bool _isPlaying;

    private void Awake()
    {
        _image = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (_playOnEnable) Play();
    }

    public void Play()
    {
        if (_frames == null || _frames.Length == 0) return;

        _index = 0;
        _timer = 0f;
        _isPlaying = true;
        _image.sprite = _frames[0];
    }

    public void Stop() => _isPlaying = false;

    private void Update()
    {
        if (!_isPlaying || _frames == null || _frames.Length == 0) return;

        _timer += Time.unscaledDeltaTime;
        float frameDuration = 1f / Mathf.Max(_frameRate, 0.0001f);

        while (_timer >= frameDuration)
        {
            _timer -= frameDuration;
            _index++;

            if (_index >= _frames.Length)
            {
                if (_loop)
                {
                    _index = 0;
                }
                else
                {
                    _index = _frames.Length - 1;
                    _isPlaying = false;
                    break;
                }
            }
        }

        _image.sprite = _frames[_index];
    }
}
