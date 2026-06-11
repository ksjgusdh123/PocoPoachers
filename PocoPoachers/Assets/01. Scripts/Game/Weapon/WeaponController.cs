using System;
using System.Collections;
using UnityEngine;

public class WeaponController : EquipableController
{
    [SerializeField] private float _switchMidTime = 0.15f;
    [SerializeField] private CrosshairUI _crosshairUI;

    public bool IsAiming    => _currentGun != null && _currentGun.IsAiming;
    public bool IsReloading => _currentGun != null && _currentGun.IsReloading;

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

    private static readonly int WeaponSwitchHash = Animator.StringToHash("WeaponSwitch");

    private WeaponMount _mount;
    private PlayerInputHandler _inputHandler;
    private Animator _animator;
    private Inventory _inventory;
    private GunBase _currentGun;
    private PlayerDodge _playerDodge;
    private int _currentGunIndex = -1;
    private bool _isSwitching;
    private bool _wasFirePressed;
    private bool _wasAimPressed;
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

        if (_inventory != null)
            _inventory.OnItemAdded += OnItemAddedToInventory;
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

    public override void Equip(ItemData data, int slotIndex)
    {
        GunBase gun = _mount.ApplyEquip(data.id, slotIndex);    
        if (gun == null) return;

        gun.Owner = gameObject;
        gun.gameObject.SetActive(false);
        OnWeaponChanged?.Invoke(slotIndex, data);
        RoomSync.Equip(data.id, slotIndex);

        if (_currentGunIndex == slotIndex) _currentGunIndex = -1;
        SwitchWeapon(slotIndex);
    }

    public override void Unequip(int slotIndex)
    {
        if (_mount.GetGun(slotIndex) == null) return;
        _mount.ApplyUnequip(slotIndex);
        if (_currentGunIndex == slotIndex) _currentGunIndex = -1;
        OnWeaponChanged?.Invoke(slotIndex, null);
        RoomSync.Equip(0, slotIndex);
    }

    public int GetEquippedItemId(int slotIndex) => _mount.GetEquippedItemId(slotIndex);


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
            prev.gameObject.SetActive(false);
        }

        _currentGunIndex = index;
        _currentGun = _mount.GetGun(index);
        _currentGun?.gameObject.SetActive(true);
        _wasFirePressed = false;

        if (_currentGun != null)
        {
            var gun = _currentGun;
            _cameraShakeHandler = _ => CameraShake.Instance?.Shake(
                gun.Stat.CameraShakeIntensity, gun.Stat.CameraShakeDuration, gun.Muzzle.up);
            _currentGun.OnShoot += _cameraShakeHandler;
            _currentGun.AimDirectionProvider = () => GetCrosshairGroundDirection(gun.Muzzle);

            _reloadRequestedHandler = () => TryReloadFromInventory();
            _reloadCompleteHandler = consumed => ConsumeAmmoFromInventory(consumed);
            _ammoChangedHandler = (cur, _) => OnAmmoChanged?.Invoke(cur, GetInventoryAmmoCount());
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

        bool isFirePressed = _inputHandler.IsFirePressed;
        bool fireInput = _currentGun.Stat.FiringMode == FiringMode.Auto
            ? isFirePressed
            : isFirePressed && !_wasFirePressed;

        if (fireInput)
        {
            _currentGun.TryShoot();
            SoundEvent.Emit(_currentGun.Muzzle.position, _currentGun.Stat.SoundRange, gameObject);
        }

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

    private void HandleCancelReloadInput()
    {
        if (_currentGun != null && _currentGun.IsReloading)
            _currentGun.CancelReload();
    }

    private void HandleAimInput()
    {
        if (_isSwitching) return;
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

    private Vector3 GetCrosshairGroundDirection(Transform muzzle)
    {
        if (CrosshairUI.Instance == null || Camera.main == null)
            return muzzle.up;

        Ray ray = Camera.main.ScreenPointToRay(CrosshairUI.Instance.ScreenPosition);
        Plane plane = new Plane(Vector3.up, new Vector3(0f, muzzle.position.y, 0f));

        if (!plane.Raycast(ray, out float distance))
            return muzzle.up;

        Vector3 targetPoint = ray.GetPoint(distance);
        Vector3 dir = targetPoint - muzzle.position;
        return dir.sqrMagnitude < 0.001f ? muzzle.up : dir.normalized;
    }
}
