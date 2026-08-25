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
    // 원격 사격 재현용 — 수신측이 이 총의 실제 총알 프리팹으로 스폰해 총기별 비주얼을 맞춘다
    public GameObject BulletPrefab => _bulletPrefab;
    // 원격 사격 재현용 — 수신측이 이 총의 총구 화염과 총성을 재생한다 (방아쇠 1회당 1번)
    public void PlayFireEffects()
    {
        // 발사 패킷이 장착/스폰보다 먼저 도착하면 스탯이 아직 없다
        if (_stat == null) return;

        _muzzleFlash?.Play(_stat.MuzzleColor);
        SoundManager.GetInstance().PlaySfxAt(_stat.FireSound, _muzzle.position, _stat.FireAudibleRange);
    }
    public float DurabilityPerShot => _durabilityDecreasePerShot;
    public Transform Muzzle => _muzzle;
    public int CurrentAmmo => _currentAmmo;
    public bool IsReloading => _isReloading;

    // 스킬 버프 — 켜져 있는 동안 발사해도 탄창이 줄지 않는다.
    // 소유자(WeaponController)가 무기를 바꿀 때마다 새 총에 다시 걸어준다.
    public bool InfiniteAmmo { get; set; }
    public GameObject Owner
    {
        get => _owner;
        set
        {
            _owner = value;
            // 재장전 게이지 등 UI는 로컬 플레이어(PlayerController 보유)의 총에만 표시
            _isLocalPlayerOwner = value != null && value.TryGetComponent<PlayerController>(out _);
            // 소유자가 적이면 사격을 적 전용 경로로 전파하기 위해 캐시 (플레이어 총알과 attacker/레이어가 다름)
            _ownerEnemy = value != null ? value.GetComponent<EnemyNetSync>() : null;
            // 플레이어 강화(공격력/공격속도)를 이 총의 기준 스탯에 반영하기 위해 캐시
            _ownerEnhancement = value != null ? value.GetComponent<PlayerEnhancement>() : null;
            ApplyOwnerCombatMultipliers();
        }
    }

    // 사격 네트워크 전파 — 소유자가 적이면 적 전용(enemyId 기반), 아니면 플레이어 경로로 보낸다.
    // 적 총알을 플레이어 경로(RoomSync.Shoot, MyId)로 보내면 게스트에서 호스트 플레이어로 오귀속돼
    // 같은 레이어(적끼리) 스킵이 깨진다 — 그래서 적은 반드시 이 분기를 타야 한다.
    protected void BroadcastShoot(Vector3 origin, Vector3 direction, System.Collections.Generic.IReadOnlyList<Vector3> pelletDirections = null, bool isHeadshot = false, System.Collections.Generic.List<int> bulletSeqs = null)
    {
        if (_ownerEnemy != null)
            RoomSync.EnemyShoot(_ownerEnemy.EnemyId, origin, direction, _stat, pelletDirections);
        else
            RoomSync.Shoot(origin, direction, _stat, pelletDirections, isHeadshot, bulletSeqs);
    }

    public static event Action<float> OnReloadStarted;
    public static event Action OnReloadEnded;

    // 호스트로부터 총 상태(H_GunState)가 적용된 직후 발생 (uid).
    // 인벤 무기 파츠 패널이 프리뷰 총을 새로고침하는 데 사용
    public static event Action<int> OnGunStateSynced;
    public static void RaiseGunStateSynced(int uid) => OnGunStateSynced?.Invoke(uid);

    public event Action<Vector2> OnShoot;
    public event Action OnReloadRequested;
    public event Action<int> OnReloadComplete;
    public event Action<int, int> OnAmmoChanged; // (현재 탄약, 최대 탄약)

    [SerializeField] private float _durabilityDecreasePerShot = 1f;

    private GameObject _owner;
    private bool _isLocalPlayerOwner;
    private EnemyNetSync _ownerEnemy;
    private PlayerEnhancement _ownerEnhancement;
    private int _currentAmmo;
    private bool _isReloading;
    private float _nextFireTime;
    private Coroutine _reloadCoroutine;

    // 즉시 장전의 시작음→마무리음 연결 재생
    private Coroutine _instantReloadSfx;

    // 재장전이 시작된 시각 — 즉시 장전이 끼어들 때 시작음의 남은 길이를 계산하는 데 쓴다
    private float _reloadStartTime;

    private Vector3 _originLocalPos;
    private float _recoilDist;

    private float _soundGizmoTimer;
    private Vector3 _soundGizmoPosition;
    private float _soundGizmoRange;

    private Camera _mainCamera;

    protected virtual void Awake()
    {
        _baseStat = DataManager.GetGunStat(_itemId).Clone();
        _stat = _baseStat.Clone();
        _bulletPrefab = Resources.Load<GameObject>(_stat.BulletPrefabPath);
        _currentAmmo = _stat.MaxMagazine;
        if (_maxDurability <= 0f) Initialize(_uid, _itemId, 100f);
        _originLocalPos = transform.localPosition;
        _mainCamera = Camera.main;
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

    public bool TryShoot()
    {
        if (_isReloading || Time.time < _nextFireTime) return false;
        if (_currentDurability <= 0f) return false;
        if (_currentAmmo <= 0)
        {
            OnReloadRequested?.Invoke();
            return false;
        }

        bool isHeadshot = HeadshotProvider?.Invoke() ?? false;
        Shoot(isHeadshot);
        _muzzleFlash?.Play(_stat.MuzzleColor);
        // 내 총은 2D로 또렷하게, 적/원격 총기는 위치 기반으로 — 리스너가 카메라라 내 총도 3D로 쏘면 감쇠된다.
        // 가청 거리는 fire_audible_range다. sound_range는 AI가 총성을 듣고 반응하는 거리라 별개로 둔다.
        if (_isLocalPlayerOwner)
            SoundManager.GetInstance().PlaySfx(_stat.FireSound);
        else
            SoundManager.GetInstance().PlaySfxAt(_stat.FireSound, _muzzle.position, _stat.FireAudibleRange);
        ShellCasingPool.Instance?.Eject(_shellEjectPort);
        _soundGizmoPosition = _muzzle.position;
        _soundGizmoRange = _stat.SoundRange;
        _soundGizmoTimer = 1f;
        if (!InfiniteAmmo)
        {
            _currentAmmo--;
            OnAmmoChanged?.Invoke(_currentAmmo, _stat.MaxMagazine);
        }
        // 로컬 낙관적 반영. 호스트만 권위 있는 내구도를 계산해 브로드캐스트한다.
        SetDurability(_currentDurability - _durabilityDecreasePerShot);
        if (RoomManager.IsHost)
            RoomSync.Durability(Uid, ItemId, -_durabilityDecreasePerShot, MaxDurability);
        _nextFireTime = Time.time + 60f / _stat.Rpm;
        _recoilDist = _stat.RecoilForce;
        Vector2 muzzleScreen    = _mainCamera.WorldToScreenPoint(_muzzle.position);
        Vector2 muzzleTipScreen = _mainCamera.WorldToScreenPoint(_muzzle.position + _muzzle.up);
        Vector2 forwardDir = (muzzleTipScreen - muzzleScreen).normalized;
        Vector2 rightDir = new Vector2(forwardDir.y, -forwardDir.x);
        Vector2 kickVector = forwardDir * _stat.CrosshairKickV
            + rightDir * UnityEngine.Random.Range(-_stat.CrosshairKickH, _stat.CrosshairKickH);
        OnShoot?.Invoke(kickVector);

        if (_currentAmmo <= 0) OnReloadRequested?.Invoke();
        return true;
    }

    // 쏜 클라가 자기 탄환에 붙이는 식별자. 호스트가 명중을 통보할 때 이 번호로 지목한다.
    // 게스트가 원격 총알을 그릴 때는 패킷으로 받은 번호를 그대로 쓰므로 여기서 발급하지 않는다.
    protected int AssignBulletSeq(Bullet bullet)
    {
        int seq = Bullet.NextSeq();
        bullet.SetNetworkId(RoomSync.MyPlayerId, seq);
        return seq;
    }

    protected abstract void Shoot(bool isHeadshot);

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
        ApplyOwnerCombatMultipliers();
    }

    // 소유자의 강화(공격력/공격속도) 배율을 기준 스탯에 곱해 넣는다.
    // RecalculateStat()이 매번 _baseStat.Clone()부터 다시 시작하므로 중복 적용될 걱정은 없다.
    private void ApplyOwnerCombatMultipliers()
    {
        if (_ownerEnhancement == null || _stat == null) return;
        _stat.damage *= _ownerEnhancement.GetBonus(EnhancementStatType.AttackPower);
        _stat.rpm    *= _ownerEnhancement.GetBonus(EnhancementStatType.AttackSpeed);
    }

    // 스탯 포인트를 새로 소비해 강화 배율이 바뀌었을 때, 이미 장착된 총에 즉시 반영하기 위해 외부(PlayerEnhancement)에서 호출
    public void RefreshEnhancementMultipliers() => RecalculateStat();

    public bool IsAiming { get; set; }

    /// <summary>
    /// 발사 기준 방향(스프레드 적용 전)을 결정하는 외부 콜백.
    /// 미설정 시 총구가 바라보는 방향(_muzzle.up)을 사용한다.
    /// 플레이어 무기는 WeaponController가 크로스헤어 기반 콜백을 주입한다.
    /// </summary>
    public Func<Vector3> AimDirectionProvider { get; set; }

    // 발사 순간 헤드샷 여부를 판정하는 외부 콜백. 미설정(적 등) 시 항상 false.
    // 플레이어 무기는 WeaponController가 크로스헤어 기반 콜백을 주입한다.
    public Func<bool> HeadshotProvider { get; set; }

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
        _reloadStartTime = Time.time;
        if (_isLocalPlayerOwner)
        {
            OnReloadStarted?.Invoke(_stat.ReloadTime);
            CrosshairUI.Instance?.StartReloadGauge(_stat.ReloadTime);
            // 재장전음은 2D 전역 재생이라 원격 플레이어/적 총기까지 울리면 거리감이 깨진다
            SoundManager.GetInstance().PlaySfx(_stat.ReloadStartSound);
        }
        yield return new WaitForSeconds(_stat.ReloadTime);
        _reloadCoroutine = null;
        FinishReload(availableAmmo);
    }

    // 재장전 모션을 건너뛰고 그 자리에서 탄창을 채운다 (스킬용).
    // 진행 중이던 재장전이 있으면 남은 대기를 버리고 완료 처리한다.
    public bool InstantReload(int availableAmmo)
    {
        if (availableAmmo <= 0 || _currentAmmo >= _stat.MaxMagazine) return false;

        bool wasReloading = _isReloading;
        if (_reloadCoroutine != null)
        {
            StopCoroutine(_reloadCoroutine);
            _reloadCoroutine = null;
        }

        // 게이지는 재장전이 실제로 떠 있던 경우에만 내린다
        if (wasReloading && _isLocalPlayerOwner)
            CrosshairUI.Instance?.StopReloadGauge();

        // 탄창은 즉시 채우되 마무리음은 여기서 내지 않는다 — 시작음과 이어 붙여야 하기 때문
        FinishReload(availableAmmo, playEndSound: false);

        if (_isLocalPlayerOwner)
        {
            if (_instantReloadSfx != null) StopCoroutine(_instantReloadSfx);
            _instantReloadSfx = StartCoroutine(InstantReloadSfxRoutine(wasReloading));
        }
        return true;
    }

    // 즉시 장전은 탄창을 곧바로 채우지만, 소리는 "장전 시작음 → 마무리음"이 이어져 들려야 한다.
    // 이미 재장전 중이었다면 시작음이 벌써 나가고 있으므로 남은 길이만큼만 기다린다.
    private IEnumerator InstantReloadSfxRoutine(bool startSoundAlreadyPlaying)
    {
        var sound = SoundManager.GetInstance();
        float startLength = sound.GetSfxLength(_stat.ReloadStartSound);
        float wait;

        if (startSoundAlreadyPlaying)
        {
            wait = Mathf.Max(0f, startLength - (Time.time - _reloadStartTime));
        }
        else
        {
            sound.PlaySfx(_stat.ReloadStartSound);
            wait = startLength;
        }

        if (wait > 0f) yield return new WaitForSeconds(wait);

        sound.PlaySfx(_stat.ReloadEndSound);
        _instantReloadSfx = null;
    }

    // 탄창 채우기 + 완료 통보. 정상 재장전과 즉시 장전이 공유한다.
    // playEndSound=false는 즉시 장전용 — 마무리음을 시작음 뒤에 붙여 내려고 호출측이 직접 재생한다.
    private void FinishReload(int availableAmmo, bool playEndSound = true)
    {
        int needed = _stat.MaxMagazine - _currentAmmo;
        int actual = Mathf.Min(needed, availableAmmo);
        _currentAmmo += actual;
        _isReloading = false;
        OnAmmoChanged?.Invoke(_currentAmmo, _stat.MaxMagazine);
        if (_isLocalPlayerOwner)
        {
            OnReloadEnded?.Invoke();
            if (playEndSound) SoundManager.GetInstance().PlaySfx(_stat.ReloadEndSound);
        }
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
