using System;
using UnityEngine;
using UnityEngine.InputSystem;

public enum PlayerInputMapType
{
    Game,
    Inventory
}

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour
{

    public Vector2 MoveInput { get; private set; }
    public bool IsSprintPressed { get; private set; }
    public bool IsFirePressed { get; private set; }
    public bool IsReloadPressed { get; private set; }
    public bool IsAimPressed { get; private set; }

    public event Action GoInventory;
    public event Action StartInteraction;
    public event Action<int> WeaponSwitch;
    public event Action<int> RegisterItemNumberKey;
    public event Action<int> ConsumeItemNumberKey;
    public event Action Dodge;

    private PlayerInput _inputMap;
    private readonly Key[] _numberKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };
    private readonly Key[] _weaponKeys = { Key.Digit7, Key.Digit8 };
    private PlayerInputMapType _inputType;

    private void Awake()
    {
        _inputMap = GetComponent<PlayerInput>();
    }

    public void SwitchInputActionMap(PlayerInputMapType type)
    {
        _inputType = type;
        _inputMap.SwitchCurrentActionMap(type.ToString());
    }

    // PlayerInput 컴포넌트가 Move 액션 발생 시 자동으로 호출
    private void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }

    private void OnSprint(InputValue value)
    {
        IsSprintPressed = value.isPressed;
    }

    private void OnFire(InputValue value)
    {
        IsFirePressed = value.isPressed;
    }

    private void OnReload(InputValue value)
    {
        IsReloadPressed = value.isPressed;
    }

    private void OnAim(InputValue value)
    {
        IsAimPressed = value.isPressed;
    }

    void OnGoInventory(InputValue value)
    {
        if (value.isPressed) GoInventory?.Invoke();
    }

    void OnInteraction(InputValue value)
    {
        if (value.isPressed) StartInteraction?.Invoke();
    }

    void OnDodge(InputValue value)
    {
        if (value.isPressed) Dodge?.Invoke();
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        for (int i = 0; i < _weaponKeys.Length; i++)
        {
            if (keyboard[_weaponKeys[i]].wasPressedThisFrame)
            {
                WeaponSwitch?.Invoke(i);
                break;
            }
        }
    }

    void OnItemNumberKey(InputValue value)
    {
        var keyboard = Keyboard.current;
        if (null == keyboard) return;

        for (int i = 0; i < _numberKeys.Length; i++)
        {
            if (keyboard[_numberKeys[i]].wasPressedThisFrame)
            {
                if (_inputType == PlayerInputMapType.Game) ConsumeItemNumberKey?.Invoke(i);
                else RegisterItemNumberKey?.Invoke(i);
                break;
            }
        }
    }
}
