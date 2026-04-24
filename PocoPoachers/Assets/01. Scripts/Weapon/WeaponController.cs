using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [SerializeField] private GunBase _currentGun;

    private void Update()
    {
        HandleFireInput();
        HandleReloadInput();
    }

    private void HandleFireInput()
    {
        if (_currentGun == null) return;

        bool fireInput = _currentGun.GunData.fireMode == FireMode.Auto
            ? Mouse.current.leftButton.isPressed
            : Mouse.current.leftButton.wasPressedThisFrame;

        if (fireInput) _currentGun.TryShoot();
    }

    private void HandleReloadInput()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
            _currentGun?.StartReload();
    }
}
