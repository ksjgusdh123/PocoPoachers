using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 수동 크랭크(F) 사용 시 잠깐 나타났다 사라지는 게이지+텍스트 전용 피드백 팝업.
// 연료 슬롯/버튼이 있는 GeneratorUI와 별개로, 발전기를 열지 않고도 바로 보여준다.
public class CrankGaugeUI : MonoBehaviour
{
    private static readonly Color CriticalColor = new Color(1f, 0.231f, 0.231f); // < 10%
    private static readonly Color LowColor = new Color(1f, 0.549f, 0f);          // < 40%
    private static readonly Color MediumColor = new Color(1f, 0.878f, 0.1f);     // < 70%
    private static readonly Color HighColor = new Color(0.2f, 0.898f, 0.4f);     // >= 70%

    public static CrankGaugeUI Instance { get; private set; }

    [SerializeField] private Slider _powerBar;
    [SerializeField] private TextMeshProUGUI _powerText;
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private float _visibleDuration = 1.5f;
    [SerializeField] private float _fadeSpeed = 8f;

    private float _visibleTimer;

    private void Awake()
    {
        Instance = this;

        if (_canvasGroup == null) return;
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        if (Generator.Instance == null) return;
        Generator.Instance.OnPowerChanged += Refresh;
    }

    private void OnDisable()
    {
        if (Generator.Instance == null) return;
        Generator.Instance.OnPowerChanged -= Refresh;
    }

    private void Update()
    {
        if (_canvasGroup == null) return;

        _visibleTimer -= Time.deltaTime;
        float target = _visibleTimer > 0f ? 1f : 0f;
        _canvasGroup.alpha = Mathf.MoveTowards(_canvasGroup.alpha, target, _fadeSpeed * Time.deltaTime);
    }

    // 크랭크를 사용한 순간 호출 — 타이머를 리셋해 다시 보이게 하고 최신 값을 표시한다.
    public void Show()
    {
        _visibleTimer = _visibleDuration;

        if (Generator.Instance != null)
            Refresh(Generator.Instance.CurrentPower, Generator.Instance.MaxPowerCapacity);
    }

    private void Refresh(float current, float max)
    {
        float ratio = max > 0f ? current / max : 0f;

        if (_powerBar != null)
            _powerBar.value = ratio;

        if (_powerText == null) return;

        string hex = ColorUtility.ToHtmlStringRGB(GetColorForRatio(ratio));
        _powerText.text = $"<color=#{hex}>{Mathf.RoundToInt(ratio * 100f)}</color><color=#B8C7D9>%</color>";
    }

    private static Color GetColorForRatio(float ratio)
    {
        if (ratio < 0.1f) return CriticalColor;
        if (ratio < 0.4f) return LowColor;
        if (ratio < 0.7f) return MediumColor;
        return HighColor;
    }
}
