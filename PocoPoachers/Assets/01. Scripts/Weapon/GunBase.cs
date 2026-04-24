using System.Collections;
using UnityEngine;

public abstract class GunBase : MonoBehaviour
{
    [SerializeField] protected GunData _gunData;
    [SerializeField] protected Transform _muzzle;

    public GunData GunData => _gunData;
    public int CurrentAmmo => _currentAmmo;
    public bool IsReloading => _isReloading;

    private int _currentAmmo;
    private bool _isReloading;
    private float _nextFireTime;

    protected virtual void Awake()
    {
        _currentAmmo = _gunData.magazineSize;
    }

    public void TryShoot()
    {
        if (_isReloading || _currentAmmo <= 0 || Time.time < _nextFireTime) return;

        Shoot();
        _currentAmmo--;
        _nextFireTime = Time.time + 1f / _gunData.fireRate;

        if (_currentAmmo <= 0) StartReload();
    }

    protected abstract void Shoot();

    public void StartReload()
    {
        if (_isReloading || _currentAmmo == _gunData.magazineSize) return;
        StartCoroutine(ReloadRoutine());
    }

    private IEnumerator ReloadRoutine()
    {
        _isReloading = true;
        yield return new WaitForSeconds(_gunData.reloadTime);
        _currentAmmo = _gunData.magazineSize;
        _isReloading = false;
    }
}
