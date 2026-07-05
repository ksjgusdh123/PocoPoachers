using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class GunBase : EquippableItemBase
{
    [SerializeField] protected Transform _muzzle;
    [SerializeField] private MuzzleFlash _muzzleFlash;
    [SerializeField] private Transform _shellEjectPort;

    protected GunStatData _stat;
    private GunStatData _baseStat;
    private readonly Dictionary<SlotType, GunPartData> _parts = new();
    protected GameObject _bulletPrefab;

    public GunStatData Stat => _stat;
    public float DurabilityPerShot => _durabilityDecreasePerShot;
    public Transform Muzzle => _muzzle;
    public int CurrentAmmo => _currentAmmo;
    public bool IsReloading => _isReloading;
    public GameObject Owner
    {
        get => _owner;
        set
        {
            _owner = value;
            // 재장전 게이지 등 UI는 로컬 플레이어(PlayerController 보유)의 총에만 표시
            _isLocalPlayerOwner = value != null && value.TryGetComponent<PlayerController>(out _);
        }
    }

    public static event Action<float> OnReloadStarted;
    public static event Action OnReloadEnded;

    public event Action<Vector2> OnShoot;
    public event Action OnReloadRequested;
    public event Action<int> OnReloadComplete;
    public event Action<int, int> OnAmmoChanged; // (현재 탄약, 최대 탄약)

    [SerializeField] private float _durabilityDecreasePerShot = 1f;

    private GameObject _owner;
    private bool _isLocalPlayerOwner;
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
        _baseStat = DataManager.GetGunStat(_itemId).Clone();
        _stat = _baseStat.Clone();
        _bulletPrefab = Resources.Load<GameObject>(_stat.BulletPrefabPath);
        _currentAmmo = _stat.MaxMagazine;
        if (_maxDurability <= 0f) Initialize(_uid, _itemId, 100f);
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

        _recoilDist = Mathf.MoveTowards(_recoilDist, 0f, _stat.RecoilRecoverySpeed * Time.deltaTime);

        Vector3 recoilDirLocal = transform.parent != null
            ? transform.parent.InverseTransformDirection(-_muzzle.up)
            : -_muzzle.up;
        transform.localPosition = _originLocalPos + recoilDirLocal * _recoilDist;
    }

    public void TryShoot()
    {
        if (_isReloading || Time.time < _nextFireTime) return;
        if (_currentDurability <= 0f) return;
        if (_currentAmmo <= 0)
        {
            OnReloadRequested?.Invoke();
            return;
        }

        Shoot();
        _muzzleFlash?.Play(_stat.MuzzleColor);
        ShellCasingPool.Instance?.Eject(_shellEjectPort);
        _soundGizmoPosition = _muzzle.position;
        _soundGizmoRange = _stat.SoundRange;
        _soundGizmoTimer = 1f;
        _currentAmmo--;
        OnAmmoChanged?.Invoke(_currentAmmo, _stat.MaxMagazine);
        // 로컬 낙관적 반영. 호스트만 권위 있는 내구도를 계산해 브로드캐스트한다.
        SetDurability(_currentDurability - _durabilityDecreasePerShot);
        if (RoomManager.IsHost)
            RoomSync.Durability(Uid, ItemId, -_durabilityDecreasePerShot, MaxDurability);
        _nextFireTime = Time.time + 60f / _stat.Rpm;
        _recoilDist = _stat.RecoilForce;
        Vector2 muzzleScreen = Camera.main.WorldToScreenPoint(_muzzle.position);
        Vector2 muzzleTipScreen = Camera.main.WorldToScreenPoint(_muzzle.position + _muzzle.up);
        Vector2 forwardDir = (muzzleTipScreen - muzzleScreen).normalized;
        Vector2 rightDir = new Vector2(forwardDir.y, -forwardDir.x);
        Vector2 kickVector = forwardDir * _stat.CrosshairKickV
            + rightDir * UnityEngine.Random.Range(-_stat.CrosshairKickH, _stat.CrosshairKickH);
        OnShoot?.Invoke(kickVector);

        if (_currentAmmo <= 0) OnReloadRequested?.Invoke();
    }

    protected abstract void Shoot();

    public void EquipPart(GunPartData part)
    {
        _parts[part.slot_type] = part;
        RecalculateStat();
    }

    public void UnequipPart(SlotType slot)
    {
        if (_parts.Remove(slot))
            RecalculateStat();
    }

    public GunPartData GetPart(SlotType slot) =>
        _parts.TryGetValue(slot, out var p) ? p : null;

    public void SetAmmo(int current)
    {
        _currentAmmo = Mathf.Clamp(current, 0, _stat.MaxMagazine);
        OnAmmoChanged?.Invoke(_currentAmmo, _stat.MaxMagazine);
    }

    private void RecalculateStat()
    {
        _stat = _baseStat.Clone();
        foreach (var part in _parts.Values)
        {
            _stat.spread             *= part.spread_multiplier;
            _stat.aim_spread         *= part.spread_multiplier;
            _stat.recoil_force       *= part.recoil_multiplier;
            _stat.reload_time        *= part.reload_time_multiplier;
            _stat.aim_fov_multiplier *= part.aim_fov_multiplier;
            _stat.max_magazine       += part.max_magazine_bonus;
            _stat.sound_range        *= part.sound_range_multiplier;
        }
    }

    public bool IsAiming { get; set; }

    /// <summary>
    /// 발사 기준 방향(스프레드 적용 전)을 결정하는 외부 콜백.
    /// 미설정 시 총구가 바라보는 방향(_muzzle.up)을 사용한다.
    /// 플레이어 무기는 WeaponController가 크로스헤어 기반 콜백을 주입한다.
    /// </summary>
    public Func<Vector3> AimDirectionProvider { get; set; }

    protected Vector3 GetFireDirection()
    {
        float spread = IsAiming ? _stat.AimSpread : _stat.Spread;
        Vector3 baseDir = GetBaseAimDirection();
        if (spread <= 0f) return baseDir;
        float angle = UnityEngine.Random.Range(-spread / 2f, spread / 2f);
        return Quaternion.AngleAxis(angle, Vector3.up) * baseDir;
    }

    private Vector3 GetBaseAimDirection()
    {
        if (AimDirectionProvider != null)
            return AimDirectionProvider();

        Vector3 dir = _muzzle.up;
        dir.y = 0f;
        return dir.sqrMagnitude < 0.001f ? _muzzle.up : dir.normalized;
    }

    public void StartReload(int availableAmmo)
    {
        if (_isReloading || _currentAmmo == _stat.MaxMagazine || availableAmmo <= 0) return;
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
        if (_isLocalPlayerOwner)
        {
            CrosshairUI.Instance?.StopReloadGauge();
            OnReloadEnded?.Invoke();
        }
    }

    private IEnumerator ReloadRoutine(int availableAmmo)
    {
        _isReloading = true;
        if (_isLocalPlayerOwner)
        {
            OnReloadStarted?.Invoke(_stat.ReloadTime);
            CrosshairUI.Instance?.StartReloadGauge(_stat.ReloadTime);
        }
        yield return new WaitForSeconds(_stat.ReloadTime);
        int needed = _stat.MaxMagazine - _currentAmmo;
        int actual = Mathf.Min(needed, availableAmmo);
        _currentAmmo += actual;
        _isReloading = false;
        _reloadCoroutine = null;
        OnAmmoChanged?.Invoke(_currentAmmo, _stat.MaxMagazine);
        if (_isLocalPlayerOwner) OnReloadEnded?.Invoke();
        OnReloadComplete?.Invoke(actual);
    }

    public bool TryAuthorizeHostShot()
    {
        if (_isReloading || Time.time < _nextFireTime) return false;
        if (_currentDurability <= 0f || _currentAmmo <= 0) return false;

        _currentAmmo--;
        OnAmmoChanged?.Invoke(_currentAmmo, _stat.MaxMagazine);
        _nextFireTime = Time.time + 60f / _stat.Rpm;
        return true;
    }

    // 호스트가 G_Shoot를 거부했을 때 권위 있는 탄약/내구도로 맞춤
    public void ApplyHostShootState(int ammo, float durability)
    {
        _currentAmmo = Mathf.Clamp(ammo, 0, _stat.MaxMagazine);
        SetDurability(durability);
        OnAmmoChanged?.Invoke(_currentAmmo, _stat.MaxMagazine);
    }

    private void OnDrawGizmos()
    {
        if (_soundGizmoTimer <= 0f) return;

        Gizmos.color = new Color(1f, 0.5f, 0f, _soundGizmoTimer);
        Gizmos.DrawWireSphere(_soundGizmoPosition, _soundGizmoRange);
    }
}
