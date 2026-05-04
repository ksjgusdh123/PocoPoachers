using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private GunBase[] _guns;

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

    private GunBase _currentGun;
    private int _currentGunIndex = -1;
    private PlayerInputHandler _inputHandler;
    private bool _wasFirePressed;
    private bool _wasAimPressed;

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
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
        if (index >= _guns.Length || index == _currentGunIndex) return;

        _guns[_currentGunIndex >= 0 ? _currentGunIndex : 0]?.gameObject.SetActive(false);

        _currentGunIndex = index;
        _currentGun = _guns[index];
        _currentGun?.gameObject.SetActive(true);

        _wasFirePressed = false;

        if (_wasAimPressed)
        {
            _wasAimPressed = false;
            if (_currentGun != null) _currentGun.IsAiming = false;
            CameraZoom.Instance?.SetAiming(false, 45f, 0.2f);
        }
    }

    private void HandleFireInput()
    {
        if (_currentGun == null) return;

        bool isFirePressed = _inputHandler.IsFirePressed;

        bool fireInput = _currentGun.GunData.fireMode == FireMode.Auto
            ? isFirePressed
            : isFirePressed && !_wasFirePressed;

        if (fireInput) _currentGun.TryShoot();

        _wasFirePressed = isFirePressed;
    }

    private void HandleReloadInput()
    {
        if (_inputHandler.IsReloadPressed)
        {
            _currentGun?.StartReload();
        }
    }

    private void HandleAimInput()
    {
        bool isAimPressed = _inputHandler.IsAimPressed;
        if (isAimPressed == _wasAimPressed) return;

        _wasAimPressed = isAimPressed;
        if (_currentGun != null) _currentGun.IsAiming = isAimPressed;
        CameraZoom.Instance?.SetAiming(
            isAimPressed,
            _currentGun != null ? _currentGun.GunData.aimFOV : 45f,
            _currentGun != null ? _currentGun.GunData.aimTime : 0.2f);
    }
}
