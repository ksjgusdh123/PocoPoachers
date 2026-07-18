using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GeneratorUI : MonoBehaviour
{
    private static readonly Color CriticalColor = new Color(1f, 0.231f, 0.231f); // < 10%
    private static readonly Color LowColor = new Color(1f, 0.549f, 0f);          // < 40%
    private static readonly Color MediumColor = new Color(1f, 0.878f, 0.1f);     // < 70%
    private static readonly Color HighColor = new Color(0.2f, 0.898f, 0.4f);     // >= 70%

    [SerializeField] private Slider _powerBar;
    [SerializeField] private TextMeshProUGUI _powerText;

    private void OnEnable()
    {
        if (Generator.Instance == null) return;

        Generator.Instance.OnPowerChanged += Refresh;
        Refresh(Generator.Instance.CurrentPower, Generator.Instance.MaxPowerCapacity);
    }

    private void OnDisable()
    {
        if (Generator.Instance == null) return;

        Generator.Instance.OnPowerChanged -= Refresh;
    }

    public void Open(PlayerController player)
    {
        if (Generator.Instance == null) return;

        Refresh(Generator.Instance.CurrentPower, Generator.Instance.MaxPowerCapacity);
    }

    private void Refresh(float current, float max)
    {
        float ratio = max > 0f ? current / max : 0f;

        if (_powerBar != null)
            _powerBar.value = ratio;

        if (_powerText != null)
        {
            _powerText.text = $"{Mathf.RoundToInt(ratio * 100f)}%";
            _powerText.color = GetColorForRatio(ratio);
        }
    }

    private static Color GetColorForRatio(float ratio)
    {
        if (ratio < 0.1f) return CriticalColor;
        if (ratio < 0.4f) return LowColor;
        if (ratio < 0.7f) return MediumColor;
        return HighColor;
    }
}
