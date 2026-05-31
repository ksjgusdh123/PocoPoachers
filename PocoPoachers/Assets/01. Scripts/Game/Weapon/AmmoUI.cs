using TMPro;
using UnityEngine;

public class AmmoUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _currentAmmoText;
    [SerializeField] private TextMeshProUGUI _maxAmmoText;

    private void OnEnable()
    {
        WeaponController.OnAmmoChanged += UpdateAmmoDisplay;
    }

    private void OnDisable()
    {
        WeaponController.OnAmmoChanged -= UpdateAmmoDisplay;
    }

    private void UpdateAmmoDisplay(int current, int max)
    {
        _currentAmmoText.text = current.ToString();
        _maxAmmoText.text = max.ToString();
    }
}
