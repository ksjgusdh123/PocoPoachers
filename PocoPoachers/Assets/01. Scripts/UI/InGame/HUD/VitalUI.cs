using UnityEngine;
using UnityEngine.UI;

public class VitalUI : MonoBehaviour
{
    [SerializeField] private Image _fillImage;

    private PlayerStat _stat;

    public void Setup(PlayerStat stat)
    {
        _stat = stat;

        _stat.OnBatteryChanged += Refresh;
        Refresh(_stat.CurrentBattery, _stat.MaxBattery);
    }

    private void OnDestroy()
    {
        if (_stat == null) return;

        _stat.OnBatteryChanged -= Refresh;
    }

    private void Refresh(float current, float max)
    {
        _fillImage.fillAmount = max > 0f ? current / max : 0f;
    }
}
