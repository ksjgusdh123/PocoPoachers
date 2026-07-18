using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GeneratorUI : MonoBehaviour
{
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
        if (_powerBar != null)
            _powerBar.value = max > 0f ? current / max : 0f;

        if (_powerText != null)
            _powerText.text = $"{current:0} / {max:0}";
    }
}
