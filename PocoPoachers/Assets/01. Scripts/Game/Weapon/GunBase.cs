using System;
using System.Collections;
using UnityEngine;

public abstract class GunBase : MonoBehaviour
{
    [SerializeField] protected GunData _gunData;
    [SerializeField] protected Transform _muzzle;
    [SerializeField] private MuzzleFlash _muzzleFlash;
    [SerializeField] private Transform _shellEjectPort;

    public GunData GunData => _gunData;
    public Transform Muzzle => _muzzle;
    public int CurrentAmmo => _currentAmmo;
    public bool IsReloading => _isReloading;
    public ItemData ItemData => ItemTable.Instance.Get(_gunData.itemId);
    public GameObject Owner { get; set; }

    public static event Action<float> OnReloadStarted;
    public static event Action OnReloadEnded;

    public event Action<Vector2> OnShoot;
    public event Action OnReloadRequested;
    public event Action<int> OnReloadComplete;
    public event Action<int, int> OnAmmoChanged; // (현재 탄약, 최대 탄약)

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
            OnReloadRequested?.Invoke();
            return;
        }

        Shoot();
        _muzzleFlash?.Play();
        ShellCasingPool.Instance?.Eject(_shellEjectPort);
        _soundGizmoPosition = _muzzle.position;
        _soundGizmoRange = _gunData.soundRange;
        _soundGizmoTimer = 1f;
        _currentAmmo--;
        OnAmmoChanged?.Invoke(_currentAmmo, _gunData.magazineSize);
        _nextFireTime = Time.time + 1f / _gunData.fireRate;
        _recoilDist = _gunData.recoilDistance;
        Vector2 muzzleScreen = Camera.main.WorldToScreenPoint(_muzzle.position);
        Vector2 muzzleTipScreen = Camera.main.WorldToScreenPoint(_muzzle.position + _muzzle.up);
        Vector2 forwardDir = (muzzleTipScreen - muzzleScreen).normalized;
        Vector2 rightDir = new Vector2(forwardDir.y, -forwardDir.x);
        Vector2 kickVector = forwardDir * _gunData.crosshairVerticalKick
            + rightDir * UnityEngine.Random.Range(-_gunData.crosshairHorizontalKick, _gunData.crosshairHorizontalKick);
        OnShoot?.Invoke(kickVector);

        if (_currentAmmo <= 0) OnReloadRequested?.Invoke();
    }

    protected abstract void Shoot();

    public bool IsAiming { get; set; }


    protected Vector3 GetFireDirection()
    {
        float spread = IsAiming ? _gunData.aimSpreadAngle : _gunData.spreadAngle;
        Vector3 baseDir = GetCrosshairGroundDirection();
        if (spread <= 0f) return baseDir;
        float angle = UnityEngine.Random.Range(-spread / 2f, spread / 2f);
        return Quaternion.AngleAxis(angle, Vector3.up) * baseDir;
    }

    private Vector3 GetCrosshairGroundDirection()
    {
        if (CrosshairUI.Instance == null || Camera.main == null)
            return _muzzle.up;

        Ray ray = Camera.main.ScreenPointToRay(CrosshairUI.Instance.ScreenPosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, _muzzle.position.y, 0f));

        if (!plane.Raycast(ray, out float distance))
            return _muzzle.up;

        Vector3 targetPoint = ray.GetPoint(distance);
        Vector3 dir = targetPoint - _muzzle.position;
        return dir.sqrMagnitude < 0.001f ? _muzzle.up : dir.normalized;
    }

    public void StartReload(int availableAmmo)
    {
        if (_isReloading || _currentAmmo == _gunData.magazineSize || availableAmmo <= 0) return;
        _reloadCoroutine = StartCoroutine(ReloadRoutine(availableAmmo));
    }

    public void CancelReload()
    {
        if (_reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
        }

        _isReloading = false;
        CrosshairUI.Instance?.StopReloadGauge();
        OnReloadEnded?.Invoke();
    }

    private IEnumerator ReloadRoutine(int availableAmmo)
    {
        _isReloading = true;
        OnReloadStarted?.Invoke(_gunData.reloadTime);
        CrosshairUI.Instance?.StartReloadGauge(_gunData.reloadTime);
        yield return new WaitForSeconds(_gunData.reloadTime);
        int needed = _gunData.magazineSize - _currentAmmo;
        int actual = Mathf.Min(needed, availableAmmo);
        _currentAmmo += actual;
        _isReloading = false;
        _reloadCoroutine = null;
        OnAmmoChanged?.Invoke(_currentAmmo, _gunData.magazineSize);
        OnReloadEnded?.Invoke();
        OnReloadComplete?.Invoke(actual);
    }

    private void OnDrawGizmos()
    {
        if (_soundGizmoTimer <= 0f) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, _soundGizmoTimer);
        Gizmos.DrawWireSphere(_soundGizmoPosition, _soundGizmoRange);
    }
}
