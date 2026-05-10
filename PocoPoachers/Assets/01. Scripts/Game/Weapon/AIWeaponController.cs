using UnityEngine;

public class AIWeaponController : MonoBehaviour
{
    [SerializeField] private GunBase _gun;

    public GunBase Gun => _gun;
    public bool HasGun => _gun != null;

    public void TryShoot()
    {
        _gun?.TryShoot();
    }

    public void StartReload()
    {
        _gun?.StartReload();
    }

    public void EquipGun(GunBase gun)
    {
        if (_gun != null)
            _gun.gameObject.SetActive(false);

        _gun = gun;

        if (_gun != null)
            _gun.gameObject.SetActive(true);
    }
}
