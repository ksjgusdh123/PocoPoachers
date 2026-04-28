using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

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

    private static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);

    protected Vector3 GetAimDirection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!GroundPlane.Raycast(ray, out float distance))
            return _muzzle.forward;

        Vector3 hitPoint = ray.GetPoint(distance);
        Vector3 direction = (hitPoint - _muzzle.position);
        direction.y = 0f;
        return direction.normalized;
    }

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
