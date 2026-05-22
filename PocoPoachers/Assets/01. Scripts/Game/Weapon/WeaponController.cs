using System;
using System.Collections;
using UnityEngine;

public class WeaponController : EquipableController
{
    [SerializeField] private float _switchMidTime = 0.15f;
    [SerializeField] private CrosshairUI _crosshairUI;

    public float MoveSpeedMultiplier
    {
        get
        {
            if (_currentGun == null) return 1f;
            return _wasAimPressed
                ? _currentGun.GunData.moveSpeedMultiplier * _currentGun.GunData.aimMoveSpeedMultiplier
                : _currentGun.GunData.moveSpeedMultiplier;
        }
    }

    public static event Action<int, ItemData> OnWeaponChanged;

    private static readonly int WeaponSwitchHash = Animator.StringToHash("WeaponSwitch");

    private WeaponMount _mount;
    private PlayerInputHandler _inputHandler;
    private Animator _animator;
    private GunBase _currentGun;
    private int _currentGunIndex = -1;
    private bool _isSwitching;
    private bool _wasFirePressed;
    private bool _wasAimPressed;
    private Action<Vector2> _cameraShakeHandler;

    private void Awake()
    {
        _mount = GetComponent<WeaponMount>();
        _inputHandler = GetComponent<PlayerInputHandler>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (_inputHandler != null)
            _inputHandler.WeaponSwitch += SwitchWeapon;
    }

    private void OnDestroy()
    {
        if (_inputHandler != null)
            _inputHandler.WeaponSwitch -= SwitchWeapon;
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
                gun.GunData.shakeIntensity, gun.GunData.shakeDuration, gun.Muzzle.up);
            _currentGun.OnShoot += _cameraShakeHandler;

            if (_crosshairUI != null)
            {
                _currentGun.OnShoot += _crosshairUI.OnShoot;
                _crosshairUI.UpdateBaseSpread(_currentGun.GunData, false);
                _crosshairUI.ResetSpread();
            }
        }

        _isSwitching = false;
    }

    private void HandleFireInput()
    {
        if (_currentGun == null || _isSwitching) return;

        bool isFirePressed = _inputHandler.IsFirePressed;
        bool fireInput = _currentGun.GunData.fireMode == FireMode.Auto
            ? isFirePressed
            : isFirePressed && !_wasFirePressed;

        if (fireInput)
        {
            _currentGun.TryShoot();
            SoundEvent.Emit(_currentGun.Muzzle.position, _currentGun.GunData.soundRange, gameObject);
        }

        _wasFirePressed = isFirePressed;
    }

    private void HandleReloadInput()
    {
        if (_isSwitching) return;
        if (_inputHandler.IsReloadPressed)
            _currentGun?.StartReload();
    }

    private void HandleAimInput()
    {
        if (_isSwitching) return;

        if (_currentGun != null && _currentGun.IsReloading)
        {
            if (_wasAimPressed)
            {
                _wasAimPressed = false;
                _currentGun.IsAiming = false;
                _crosshairUI?.UpdateBaseSpread(_currentGun.GunData, false);
                CameraZoom.Instance?.SetAiming(false, _currentGun.GunData.aimFOV, _currentGun.GunData.aimTime);
            }
            return;
        }

        bool isAimPressed = _inputHandler.IsAimPressed;
        if (isAimPressed == _wasAimPressed) return;

        _wasAimPressed = isAimPressed;
        if (_currentGun != null) _currentGun.IsAiming = isAimPressed;
        _crosshairUI?.UpdateBaseSpread(_currentGun?.GunData, isAimPressed);
        CameraZoom.Instance?.SetAiming(
            isAimPressed,
            _currentGun != null ? _currentGun.GunData.aimFOV : 45f,
            _currentGun != null ? _currentGun.GunData.aimTime : 0.2f);
    }
}
