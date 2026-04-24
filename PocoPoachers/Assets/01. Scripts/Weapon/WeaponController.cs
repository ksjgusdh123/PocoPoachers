using UnityEngine;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private GunBase _currentGun;

    private PlayerInputHandler _inputHandler;
    private bool _wasFirePressed;

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        HandleFireInput();
        HandleReloadInput();
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
}
