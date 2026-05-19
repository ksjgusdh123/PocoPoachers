using System;
using System.Collections;
using UnityEngine;
using UnityEngine.VFX;

public abstract class GunBase : MonoBehaviour
{
    [SerializeField] protected GunData _gunData;
    [SerializeField] protected Transform _muzzle;
    [SerializeField] private VisualEffectAsset _muzzleFlashAsset;
    [SerializeField] private MuzzleFlash _muzzleFlash;

    public GunData GunData => _gunData;
    public Transform Muzzle => _muzzle;
    public int CurrentAmmo => _currentAmmo;
    public bool IsReloading => _isReloading;
    public ItemData ItemData => ItemTable.Instance.Get(_gunData.itemId);
    public GameObject Owner { get; set; }

    public event Action<Vector2> OnShoot;

    private int _currentAmmo;
    private bool _isReloading;
    private float _nextFireTime;
    private Coroutine _reloadCoroutine;

    private Vector3 _originLocalPos;
    private float _recoilDist;

    private float _soundGizmoTimer;
    private Vector3 _soundGizmoPosition;
    private float _soundGizmoRange;

    protected virtual void Awake()
    {
        _currentAmmo = _gunData.magazineSize;
        _originLocalPos = transform.localPosition;

        if (_muzzleFlash == null && _muzzleFlashAsset != null && _muzzle != null)
            _muzzleFlash = MuzzleFlash.Create(_muzzle, _muzzleFlashAsset);
    }

    protected virtual void OnDisable()
    {
        CancelReload();
    }

    private void Update()
    {
        if (_soundGizmoTimer > 0f)
            _soundGizmoTimer -= Time.deltaTime;

        if (_recoilDist <= 0f) return;

        _recoilDist = Mathf.MoveTowards(_recoilDist, 0f, _gunData.recoilReturnSpeed * Time.deltaTime);

        Vector3 recoilDirLocal = transform.parent != null
            ? transform.parent.InverseTransformDirection(-_muzzle.up)
            : -_muzzle.up;
        transform.localPosition = _originLocalPos + recoilDirLocal * _recoilDist;
    }

    public void TryShoot()
    {
        if (_isReloading || Time.time < _nextFireTime) return;
        if (_currentAmmo <= 0)
        {
            StartReload();
            return;
        }

        Shoot();
        _muzzleFlash?.Play();
        _soundGizmoPosition = _muzzle.position;
        _soundGizmoRange = _gunData.soundRange;
        _soundGizmoTimer = 1f;
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
        _reloadCoroutine = StartCoroutine(ReloadRoutine());
    }

    public void CancelReload()
    {
        if (_reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
        }

        _isReloading = false;
    }

    private IEnumerator ReloadRoutine()
    {
        _isReloading = true;
        yield return new WaitForSeconds(_gunData.reloadTime);
        _currentAmmo = _gunData.magazineSize;
        _isReloading = false;
        _reloadCoroutine = null;
    }

    private void OnDrawGizmos()
    {
        if (_soundGizmoTimer <= 0f) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, _soundGizmoTimer);
        Gizmos.DrawWireSphere(_soundGizmoPosition, _soundGizmoRange);
    }
}
