using UnityEngine;
using UnityEngine.UI;

public class VitalUI : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private bool _isHunger;

    private PlayerStat _stat;

    public void Setup(PlayerStat stat)
    {
        _stat = stat;

        if (_isHunger)
        {
            _stat.OnHungerChanged += Refresh;
            Refresh(_stat.CurrentHunger, _stat.MaxHunger);
        }
        else
        {
            _stat.OnThirstChanged += Refresh;
            Refresh(_stat.CurrentThirst, _stat.MaxThirst);
        }
    }

    private void Refresh(float current, float max)
    {
        _fillImage.fillAmount = max > 0f ? current / max : 0f;
    }
}
