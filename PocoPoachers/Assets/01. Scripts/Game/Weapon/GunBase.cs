using System;
using System.Collections;
using UnityEngine;

public abstract class GunBase : MonoBehaviour
{
    [SerializeField] protected GunData _gunData;
    [SerializeField] protected Transform _muzzle;

    public GunData GunData => _gunData;
    public int CurrentAmmo => _currentAmmo;
    public bool IsReloading => _isReloading;

    public event Action<Vector2> OnShoot;

    private int _currentAmmo;
    private bool _isReloading;
    private float _nextFireTime;

    private Vector3 _originLocalPos;
    private float _recoilDist;

    protected virtual void Awake()
    {
        _currentAmmo = _gunData.magazineSize;
        _originLocalPos = transform.localPosition;
    }

    private void Update()
    {
        if (_recoilDist <= 0f) return;

        _recoilDist = Mathf.MoveTowards(_recoilDist, 0f, _gunData.recoilReturnSpeed * Time.deltaTime);

        Vector3 recoilDirLocal = transform.parent != null
            ? transform.parent.InverseTransformDirection(-_muzzle.up)
            : -_muzzle.up;
        transform.localPosition = _originLocalPos + recoilDirLocal * _recoilDist;
    }

    public void TryShoot()
    {
        if (_isReloading || _currentAmmo <= 0 || Time.time < _nextFireTime) return;

        Shoot();
        _currentAmmo--;
        _nextFireTime = Time.time + 1f / _gunData.fireRate;
        _recoilDist = _gunData.recoilDistance;
        Vector2 muzzleScreen = Camera.main.WorldToScreenPoint(_muzzle.position);
        Vector2 muzzleTipScreen = Camera.main.WorldToScreenPoint(_muzzle.position + _muzzle.up);
        Vector2 forwardDir = (muzzleTipScreen - muzzleScreen).normalized;
        Vector2 rightDir = new Vector2(forwardDir.y, -forwardDir.x);
        Vector2 kickVector = forwardDir * _gunData.crosshairVerticalKick
            + rightDir * UnityEngine.Random.Range(-_gunData.crosshairHorizontalKick, _gunData.crosshairHorizontalKick);
        OnShoot?.Invoke(kickVector);
        CameraShake.Instance?.Shake(_gunData.shakeIntensity, _gunData.shakeDuration, _muzzle.up);

        if (_currentAmmo <= 0) StartReload();
    }

    protected abstract void Shoot();

    public bool IsAiming { get; set; }

    protected Vector3 GetFireDirection()
    {
        float spread = IsAiming ? _gunData.aimSpreadAngle : _gunData.spreadAngle;
        if (spread <= 0f) return _muzzle.up;
        float angle = UnityEngine.Random.Range(-spread / 2f, spread / 2f);
        return Quaternion.AngleAxis(angle, Vector3.up) * _muzzle.up;
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
