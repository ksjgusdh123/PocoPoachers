using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour
{
    public const float DoubleClickThreshold = 0.3f;
    public Vector2 MoveInput { get; private set; }

    public event Action GoInventory;
    public event Action StartInteraction;
    public event Action<int> ItemNumberKey;
    public event Action DoubleClick;

    private readonly Key[] _numberKeys = { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5 };
    private float _lastClickTime;

    // PlayerInput 컴포넌트가 Move 액션 발생 시 자동으로 호출
    private void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }
     
    void OnGoInventory(InputValue value)
    {
        if (value.isPressed) GoInventory?.Invoke();
    }

    void OnInteraction(InputValue value)
    {
        if (value.isPressed) StartInteraction?.Invoke();
    }

    void OnClick(InputValue value)
    {
        if (!value.isPressed) return;
        if (Time.time - _lastClickTime < DoubleClickThreshold)
            DoubleClick?.Invoke();
        _lastClickTime = Time.time;
    }

    void OnItemNumberKey(InputValue value)
    {
        var keyboard = Keyboard.current;
        if (null == keyboard) return;

        for (int i = 0; i < _numberKeys.Length; i++)
        {
            if (keyboard[_numberKeys[i]].wasPressedThisFrame)
            {
                ItemNumberKey?.Invoke(i);
                break;
            }
        }
    }
}
