using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private GunBase _currentGun;

    private PlayerInputHandler _inputHandler;
    private bool _wasFirePressed;
    private bool _wasAimPressed;

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        HandleFireInput();
        HandleReloadInput();
        HandleAimInput();
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
        CameraZoom.Instance?.SetAiming(isAimPressed, _currentGun != null ? _currentGun.GunData.aimFOV : 45f);
    }
}
