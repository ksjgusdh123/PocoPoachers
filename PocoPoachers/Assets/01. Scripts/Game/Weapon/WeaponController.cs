using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private GunBase[] _guns;
    [SerializeField] private float _switchMidTime = 0.15f;
    [SerializeField] private CrosshairUI _crosshairUI;
    [SerializeField] private Transform _mountPoint;
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

    private static readonly int WeaponSwitchHash = Animator.StringToHash("WeaponSwitch");

    private GunBase _currentGun;
    private int _currentGunIndex = -1;
    private bool _isSwitching;
    private PlayerInputHandler _inputHandler;
    private Animator _animator;
    private bool _wasFirePressed;
    private bool _wasAimPressed;
    private System.Action<Vector2> _cameraShakeHandler;

    private void Awake()
    {
        _guns = new GunBase[2];
        _inputHandler = GetComponent<PlayerInputHandler>();
        _animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        _inputHandler.WeaponSwitch += SwitchWeapon;
        //if (_guns.Length > 0) SwitchWeapon(0);
    }

    private void OnDestroy()
    {
        _inputHandler.WeaponSwitch -= SwitchWeapon;
    }

    private void Update()
    {
        HandleFireInput();
        HandleReloadInput();
        HandleAimInput();
    }

    public void EquipWeapon(int itemId, int index)
    {
        if (_guns[index] != null)
            Destroy(_guns[index].gameObject);

        GunBase equipped = GunTable.Instance.Equip(itemId, _mountPoint);
        if (equipped == null) return;

        equipped.gameObject.SetActive(false);
        _guns[index] = equipped;
    }

    private void SwitchWeapon(int index)
    {
        if (index >= _guns.Length || index == _currentGunIndex || _isSwitching) return;
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

        _animator.SetTrigger(WeaponSwitchHash);

        yield return new WaitForSeconds(_switchMidTime);

        if (_currentGunIndex >= 0 && _guns[_currentGunIndex] != null)
        {
            if (_crosshairUI != null) _guns[_currentGunIndex].OnShoot -= _crosshairUI.OnShoot;
            if (_cameraShakeHandler != null) _guns[_currentGunIndex].OnShoot -= _cameraShakeHandler;
        }

        _guns[_currentGunIndex >= 0 ? _currentGunIndex : 0]?.gameObject.SetActive(false);
        _currentGunIndex = index;
        _currentGun = _guns[index];
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

        if (fireInput) _currentGun.TryShoot();

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
