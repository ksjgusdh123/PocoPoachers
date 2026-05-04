using System.Collections;
using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private GunBase[] _guns;
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

    private static readonly int WeaponSwitchHash = Animator.StringToHash("WeaponSwitch");

    private GunBase _currentGun;
    private int _currentGunIndex = -1;
    private bool _isSwitching;
    private PlayerInputHandler _inputHandler;
    private Animator _animator;
    private bool _wasFirePressed;
    private bool _wasAimPressed;

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
        _animator = GetComponentInChildren<Animator>();
        foreach (var gun in _guns)
            gun?.gameObject.SetActive(false);
    }

    private void Start()
    {
        _inputHandler.WeaponSwitch += SwitchWeapon;
        if (_guns.Length > 0) SwitchWeapon(0);
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

        if (_currentGunIndex >= 0 && _guns[_currentGunIndex] != null && _crosshairUI != null)
            _guns[_currentGunIndex].OnShoot -= _crosshairUI.OnShoot;

        _guns[_currentGunIndex >= 0 ? _currentGunIndex : 0]?.gameObject.SetActive(false);
        _currentGunIndex = index;
        _currentGun = _guns[index];
        _currentGun?.gameObject.SetActive(true);
        _wasFirePressed = false;

        if (_currentGun != null && _crosshairUI != null)
        {
            _currentGun.OnShoot += _crosshairUI.OnShoot;
            _crosshairUI.UpdateBaseSpread(_currentGun.GunData, false);
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
