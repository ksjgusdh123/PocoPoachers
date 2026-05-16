using System;
using System.Collections;
using UnityEngine;

public abstract class GunBase : MonoBehaviour
{
    [SerializeField] protected GunData _gunData;
    [SerializeField] protected Transform _muzzle;

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

    protected void BroadcastShoot(Vector3 origin, Vector3 direction)
    {
        if (RoomManager.IsHost && !RoomManager.HasGuests) return;

        int myId = NetworkManager.Instance?.MyPlayerId ?? 0;
        var originT = new Vec3T { X = origin.x, Y = origin.y, Z = origin.z };
        var dirT = new Vec3T { X = direction.x, Y = direction.y, Z = direction.z };

        if (RoomManager.IsHost)
        {
            PacketBuilder.BroadcastToGuests(new H_ShootT
            {
                PlayerId    = myId,
                Origin      = originT,
                Direction   = dirT,
                BulletSpeed = _gunData.bulletSpeed,
                Damage      = _gunData.damage,
                MaxRange    = _gunData.range,
            }, H_Shoot.Pack, PacketType.H_Shoot);
        }
        else
        {
            PacketBuilder.SendToHost(new G_ShootT
            {
                PlayerId    = myId,
                Origin      = originT,
                Direction   = dirT,
                BulletSpeed = _gunData.bulletSpeed,
                Damage      = _gunData.damage,
                MaxRange    = _gunData.range,
            }, G_Shoot.Pack, PacketType.G_Shoot);
        }
    }

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
