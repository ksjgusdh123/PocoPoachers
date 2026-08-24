using System;
using System.Collections;
using UnityEngine;

public class WeaponController : EquipableController
{
    [SerializeField] private float _switchMidTime = 0.15f;
    [SerializeField] private CrosshairUI _crosshairUI;
    [SerializeField] private LayerMask _aimHitMask;
    [SerializeField] private LayerMask _headHitMask;
    [SerializeField] private float _maxAimDistance = 200f;
    [SerializeField] private float _aimHeightSampleRadius = 0.25f; // 크로스헤어 광선 주변 이 반경 안에 hitMask 오브젝트가 있으면 그 Y좌표로 조준 높이를 보정

    private static readonly string[] AimHitLayerNames =
        { "Ground", "Terrain", "Wall", "Enemy", "Sandbag", "ItemBox", "Storage", "Ore" };

    public bool IsAiming    => _currentGun != null && _currentGun.IsAiming;
    public bool IsReloading => _currentGun != null && _currentGun.IsReloading;
    public GunBase CurrentGun => _currentGun;

    // 슬롯에 장착된 총 (없으면 null) — 파츠 패널 등 외부에서 조회
    public GunBase GetGun(int slot) => _mount != null ? _mount.GetGun(slot) : null;

    public float MoveSpeedMultiplier
    {
        get
        {
            if (_currentGun == null) return 1f;
            return _wasAimPressed
                ? _currentGun.Stat.MoveSpeedMultiplier * _currentGun.Stat.AimMoveSpeedMultiplier
                : _currentGun.Stat.MoveSpeedMultiplier;
        }
    }

    public static event Action<int, ItemData> OnWeaponChanged; // ItemData가 null이면 언이큅
    public static event Action<int, int> OnAmmoChanged; // (현재 탄약, 인벤토리 잔여 탄약)
    public static event Action<int> OnWeaponSwitched;  // (슬롯 인덱스)

    private const int WeaponSlotCount = 2;  // 무기 장착 슬롯 수 (0, 1)

    private static readonly int WeaponSwitchHash = Animator.StringToHash("WeaponSwitch");

    private WeaponMount _mount;
    private PlayerInputHandler _inputHandler;
    private Animator _animator;
    private Inventory _inventory;
    private GunBase _currentGun;
    private PlayerDodge _playerDodge;
    private PlayerMovement _playerMovement;
    private int _currentGunIndex = -1;
    private bool _isSwitching;
    private bool _wasFirePressed;
    private bool _wasAimPressed;
    private bool _isCurrentGunVisible = true;
    private Action<Vector2> _cameraShakeHandler;
    private Action _reloadRequestedHandler;
    private Action<int> _reloadCompleteHandler;
    private Action<int, int> _ammoChangedHandler;

    private void Awake()
    {
        _mount = GetComponent<WeaponMount>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        _animator = GetComponentInChildren<Animator>();
        _inventory = GetComponent<Inventory>();
        _playerDodge = GetComponent<PlayerDodge>();
        _playerMovement = GetComponent<PlayerMovement>();

        // Player는 씬마다 PlayerSpawner가 새로 Instantiate하므로 프리팹 자체에는 씬의
        // CrosshairUI를 인스펙터 참조로 담아둘 수 없다. 비어있으면 씬에서 찾아 채운다.
        if (_crosshairUI == null)
            _crosshairUI = FindAnyObjectByType<CrosshairUI>(FindObjectsInactive.Include);

        if (_inventory != null)
            _inventory.OnItemAdded += OnItemAddedToInventory;

        EnsureAimHitMask();
        EnsureHeadHitMask();
    }

    // 새로 추가된 필드라 기존 씬/프리팹에는 0(Nothing)으로 직렬화돼 있음 — 수동 설정 안 됐으면 기본 레이어로 채움
    private void EnsureAimHitMask()
    {
        if (_aimHitMask.value != 0) return;
        _aimHitMask = LayerMask.GetMask(AimHitLayerNames);
    }

    private void EnsureHeadHitMask()
    {
        if (_headHitMask.value != 0) return;
        _headHitMask = LayerMask.GetMask("Head");
    }

    private void Start()
    {
        if (_inputHandler != null)
        {
            _inputHandler.WeaponSwitch += SwitchWeapon;
            _inputHandler.CancelReload += HandleCancelReloadInput;
        }
    }

    private void OnDestroy()
    {
        if (_inputHandler != null)
        {
            _inputHandler.WeaponSwitch -= SwitchWeapon;
            _inputHandler.CancelReload -= HandleCancelReloadInput;
        }
        if (_inventory != null)
            _inventory.OnItemAdded -= OnItemAddedToInventory;
    }

    private void Update()
    {
        if (_inputHandler == null) return;
        HandleFireInput();
        HandleReloadInput();
        HandleAimInput();
    }

    public override void Equip(ItemData data, int slotIndex, int uid)
    {
        GunBase gun = _mount.ApplyEquip(data.id, slotIndex, uid);
        if (gun == null) return;

        // 호스트(싱글플레이 포함) 본인이 장착하는 경우, 기존 내구도를 바로 조회해서 복원
        // 게스트는 여기서 복원하지 않는다 — RoomSync.Equip(G_Equip)에 대한 응답으로
        // 호스트가 H_Durability(내구도)와 H_GunState(탄약/파츠)를 되돌려준다
        if (RoomManager.IsHost && uid != 0)
        {
            var (current, _) = WorldEquipmentManager.GetOrCreate(uid, data.id, gun.MaxDurability);
            gun.SetDurability(current);

            // 저장된 파츠 복원 — EquipPart가 RecalculateStat을 호출해 최대 장탄수도 갱신됨
            foreach (var kv in WorldEquipmentManager.GetParts(uid))
            {
                var part = GunPartTable.Instance.Get(kv.Value);
                if (part != null)
                {
                    int partUid = WorldEquipmentManager.GetPartUid(uid, kv.Key);
                    gun.EquipPart(WorldEquipmentManager.GetEnhancedGunPart(part, partUid));
                }
            }
            // 파츠 복원 후 현재 장탄수 복원
            bool hasSavedAmmo = WorldEquipmentManager.TryGetAmmo(uid, out int curAmmo, out _); // TODO: 디버그 후 제거
            Debug.Log($"[AmmoRestore] 장착 uid={uid} 저장탄약있음={hasSavedAmmo} 복원값={(hasSavedAmmo ? curAmmo : gun.CurrentAmmo)} (없으면 풀장전)"); // TODO: 디버그 후 제거
            if (hasSavedAmmo)
                gun.SetAmmo(curAmmo);
        }
        else if (uid != 0) Debug.Log($"[AmmoRestore] 게스트 장착 uid={uid} — 호스트의 H_GunState 대기"); // TODO: 디버그 후 제거

        gun.Owner = gameObject;
        gun.gameObject.SetActive(false);
        OnWeaponChanged?.Invoke(slotIndex, data);
        RoomSync.Equip(data.id, slotIndex, uid);

        if (_currentGunIndex == slotIndex) _currentGunIndex = -1;
        SwitchWeapon(slotIndex);
    }

    public override void Unequip(int slotIndex)
    {
        GunBase gun = _mount.GetGun(slotIndex);
        if (gun == null) return;

        // 해제 시점의 탄약을 저장해둬야 나중에 재장착할 때 복원할 수 있음 (파츠 장착 시 저장 로직과 동일 목적)
        RoomSync.GunAmmoSave(gun.Uid, gun.CurrentAmmo, gun.Stat.MaxMagazine);

        _mount.ApplyUnequip(slotIndex);
        if (_currentGunIndex == slotIndex) _currentGunIndex = -1;
        OnWeaponChanged?.Invoke(slotIndex, null);
        RaiseUnequipped(slotIndex);
        RoomSync.Equip(0, slotIndex, 0);
    }

    public override void UnequipAll()
    {
        // 무기 슬롯 0, 1 모두 해제
        for (int i = 0; i < WeaponSlotCount; i++)
            Unequip(i);
    }

    public int GetEquippedItemId(int slotIndex) => _mount != null ? _mount.GetEquippedItemId(slotIndex) : 0;

    public override int GetEquippedId(int slotIndex) => _mount != null ? _mount.GetEquippedItemId(slotIndex) : 0;
    public override int GetEquippedUid(int slotIndex) => _mount != null ? _mount.GetEquippedUid(slotIndex) : 0;


    private void SwitchWeapon(int index)
    {
        if (index >= 2 || index == _currentGunIndex || _isSwitching) return;
        if (_mount.GetGun(index) == null) return;
        StartCoroutine(SwitchWeaponRoutine(index));
    }

    private IEnumerator SwitchWeaponRoutine(int index)
    {
        _isSwitching = true;

        if (_wasAimPressed)
        {
            _wasAimPressed = false;
            if (_currentGun != null) _currentGun.IsAiming = false;
            CameraZoom.Instance?.SetAiming(false, 45f, 0.2f);
        }

        _animator?.SetTrigger(WeaponSwitchHash);

        yield return new WaitForSeconds(_switchMidTime);

        GunBase prev = _currentGunIndex >= 0 ? _mount.GetGun(_currentGunIndex) : null;
        if (prev != null)
        {
            if (_crosshairUI != null) prev.OnShoot -= _crosshairUI.OnShoot;
            if (_cameraShakeHandler != null) prev.OnShoot -= _cameraShakeHandler;
            if (_reloadRequestedHandler != null) prev.OnReloadRequested -= _reloadRequestedHandler;
            if (_reloadCompleteHandler != null) prev.OnReloadComplete -= _reloadCompleteHandler;
            if (_ammoChangedHandler != null) prev.OnAmmoChanged -= _ammoChangedHandler;
            prev.AimDirectionProvider = null;
            prev.HeadshotProvider = null;
            prev.gameObject.SetActive(false);
        }

        _currentGunIndex = index;
        _currentGun = _mount.GetGun(index);
        _mount.SetActiveSlot(index);
        _currentGun?.gameObject.SetActive(true);
        ApplyCurrentGunVisibility();
        _wasFirePressed = false;

        if (_currentGun != null)
        {
            var gun = _currentGun;
            _cameraShakeHandler = _ => CameraShake.Instance?.Shake(
                gun.Stat.CameraShakeIntensity, gun.Stat.CameraShakeDuration, gun.Muzzle.up);
            _currentGun.OnShoot += _cameraShakeHandler;
            _currentGun.AimDirectionProvider = () => GetCrosshairAimDirection(gun.Muzzle);
            _currentGun.HeadshotProvider = CheckHeadshot;

            _reloadRequestedHandler = () => TryReloadFromInventory();
            _reloadCompleteHandler = consumed => ConsumeAmmoFromInventory(consumed);
            _ammoChangedHandler = (cur, max) =>
            {
                // 현재 탄약을 영속 저장소에 반영해 씬 전환 후 재장착 시 복원되게 한다 (호스트 권위)
                if (RoomManager.IsHost && gun.Uid != 0)
                    WorldEquipmentManager.SetAmmo(gun.Uid, cur, max);
                OnAmmoChanged?.Invoke(cur, GetInventoryAmmoCount());
            };
            _currentGun.OnReloadRequested += _reloadRequestedHandler;
            _currentGun.OnReloadComplete += _reloadCompleteHandler;
            _currentGun.OnAmmoChanged += _ammoChangedHandler;

            // 총 교체 시 현재 탄약 수 및 슬롯 위치 즉시 갱신
            OnAmmoChanged?.Invoke(_currentGun.CurrentAmmo, GetInventoryAmmoCount());
            OnWeaponSwitched?.Invoke(index);

            if (_crosshairUI != null)
            {
                _currentGun.OnShoot += _crosshairUI.OnShoot;
                _crosshairUI.UpdateBaseSpread(_currentGun.Stat, false);
                _crosshairUI.ResetSpread();
            }
        }

        _isSwitching = false;
    }

    private void HandleFireInput()
    {
        if (_currentGun == null || _isSwitching) return;
        if (_playerDodge != null && _playerDodge.IsRolling) return;
        if (_playerMovement != null && _playerMovement.IsSprinting) return;

        bool isFirePressed = _inputHandler.IsFirePressed;
        bool fireInput = _currentGun.Stat.FiringMode == FiringMode.Auto
            ? isFirePressed
            : isFirePressed && !_wasFirePressed;

        if (fireInput && _currentGun.TryShoot())
            SoundEvent.Emit(_currentGun.Muzzle.position, _currentGun.Stat.SoundRange, gameObject);

        _wasFirePressed = isFirePressed;
    }

    private void HandleReloadInput()
    {
        if (_isSwitching) return;
        if (_inputHandler.IsReloadPressed)
            TryReloadFromInventory();
    }

    private void TryReloadFromInventory()
    {
        if (_currentGun == null || _inventory == null) return;
        var ammoData = ItemTable.Instance.Get(_currentGun.Stat.AmmoItemId);
        if (ammoData == null) return;
        int available = _inventory.GetItemCount(ammoData);
        _currentGun.StartReload(available);
    }

    private void ConsumeAmmoFromInventory(int consumed)
    {
        if (_inventory == null || consumed <= 0) return;
        var ammoData = ItemTable.Instance.Get(_currentGun.Stat.AmmoItemId);
        if (ammoData == null) return;
        _inventory.RemoveItem(ammoData, consumed);
        OnAmmoChanged?.Invoke(_currentGun.CurrentAmmo, GetInventoryAmmoCount());
    }

    private int GetInventoryAmmoCount()
    {
        if (_currentGun == null || _inventory == null) return 0;
        var ammoData = ItemTable.Instance.Get(_currentGun.Stat.AmmoItemId);
        return ammoData != null ? _inventory.GetItemCount(ammoData) : 0;
    }

    private void OnItemAddedToInventory(ItemData addedItem)
    {
        if (_currentGun == null) return;
        if (addedItem.Type != ItemType.Bullet) return;
        if (addedItem.Id != _currentGun.Stat.AmmoItemId) return;
        OnAmmoChanged?.Invoke(_currentGun.CurrentAmmo, GetInventoryAmmoCount());
    }

    public void CancelReload()
    {
        _currentGun?.CancelReload();
    }

    public void SetCurrentGunVisible(bool visible)
    {
        if (_isCurrentGunVisible == visible) return;
        _isCurrentGunVisible = visible;
        ApplyCurrentGunVisibility();
    }

    private void ApplyCurrentGunVisibility()
    {
        if (_currentGun == null) return;

        foreach (Renderer renderer in _currentGun.GetComponentsInChildren<Renderer>(true))
            renderer.enabled = _isCurrentGunVisible;
    }

    private void HandleCancelReloadInput()
    {
        if (_currentGun != null && _currentGun.IsReloading)
            _currentGun.CancelReload();
    }

    private void HandleAimInput()
    {
        if (_currentGunIndex < 0 || _isSwitching) return;
        if (_playerDodge != null && _playerDodge.IsRolling)
        {
            if (_wasAimPressed)
            {
                _wasAimPressed = false;
                if (_currentGun != null) _currentGun.IsAiming = false;
                _crosshairUI?.UpdateBaseSpread(_currentGun?.Stat, false);
                CameraZoom.Instance?.SetAiming(false, GetAimFov(), GetAimTime());
            }
            return;
        }

        if (_currentGun != null && _currentGun.IsReloading)
        {
            if (_wasAimPressed)
            {
                _wasAimPressed = false;
                _currentGun.IsAiming = false;
                _crosshairUI?.UpdateBaseSpread(_currentGun.Stat, false);
                CameraZoom.Instance?.SetAiming(false, GetAimFov(), GetAimTime());
            }
            return;
        }

        bool isAimPressed = _inputHandler.IsAimPressed;
        if (isAimPressed == _wasAimPressed) return;

        _wasAimPressed = isAimPressed;
        if (_currentGun != null) _currentGun.IsAiming = isAimPressed;
        _crosshairUI?.UpdateBaseSpread(_currentGun?.Stat, isAimPressed);
        CameraZoom.Instance?.SetAiming(isAimPressed, GetAimFov(), GetAimTime());
    }

    private float GetAimFov() =>
        _currentGun != null ? _currentGun.Stat.AimFovMultiplier * CameraZoom.Instance.DefaultFOV : 45f;

    private float GetAimTime() =>
        _currentGun != null ? _currentGun.Stat.AimTime : 0.2f;

    // 크로스헤어가 실제로 가리키는 3D 지점(적/지형)을 향한 방향을 계산한다.
    // 총구 높이의 평평한 가상 평면과 교차시키던 이전 방식은 터레인 높낮이를 무시해서
    // 언덕 위 적을 조준해도 총알이 총구 높이로 수평 발사되는 문제가 있었다.
    private Vector3 GetCrosshairAimDirection(Transform muzzle)
    {
        if (CrosshairUI.Instance == null || Camera.main == null)
            return muzzle.up;

        Ray ray = Camera.main.ScreenPointToRay(CrosshairUI.Instance.ScreenPosition);

        // 광선 주변 반경 안에 hitMask 오브젝트가 있으면 그 오브젝트의 Y좌표를 조준 높이로 쓴다.
        // X/Z는 건드리지 않고(스냅 안 함) 크로스헤어 광선 그대로 유지 — 높이만 보정하는 것이 목적.
        float planeHeight = muzzle.position.y;
        if (Physics.SphereCast(ray, _aimHeightSampleRadius, out RaycastHit hit, _maxAimDistance, _aimHitMask, QueryTriggerInteraction.Ignore))
            planeHeight = hit.point.y;

        Plane plane = new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f));
        Vector3 targetPoint = plane.Raycast(ray, out float distance) ? ray.GetPoint(distance) : ray.GetPoint(_maxAimDistance);

        // 총구는 플레이어 중심에서 좌우/앞으로 오프셋돼 있어, 크로스헤어가 플레이어 가까이 있을 때
        // 총구 기준으로 방향을 잡으면 총알이 플레이어 쪽으로 꺾여 나간다.
        // 방향은 총구 높이의 플레이어 중심을 기준으로 계산하고, 실제 발사 위치(총구)는 그대로 사용한다.
        Vector3 dirOrigin = new Vector3(transform.position.x, muzzle.position.y, transform.position.z);
        Vector3 dir = targetPoint - dirOrigin;
        return dir.sqrMagnitude < 0.001f ? muzzle.up : dir.normalized;
    }

    // 발사 순간 크로스헤어 지점에서 Head 레이어로 직접 레이캐스트해 헤드샷 여부를 판정한다.
    // GunBase.HeadshotProvider로 주입되어 TryShoot() 내부에서 호출된다.
    private bool CheckHeadshot()
    {
        if (CrosshairUI.Instance == null || Camera.main == null) return false;

        Ray ray = Camera.main.ScreenPointToRay(CrosshairUI.Instance.ScreenPosition);
        bool isHeadshot = Physics.Raycast(ray, out RaycastHit hit, _maxAimDistance, _headHitMask, QueryTriggerInteraction.Collide);

        Debug.Log(isHeadshot // TODO: 디버그 후 제거
            ? $"[Headshot] 헤드샷! 대상: {hit.collider.transform.root.name}"
            : "[Headshot] 헤드샷 아님");

        return isHeadshot;
    }
}
