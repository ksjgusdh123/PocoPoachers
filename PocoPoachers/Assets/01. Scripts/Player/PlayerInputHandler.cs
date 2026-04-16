using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
public class PlayerInputHandler : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }

    // PlayerInput 컴포넌트가 Move 액션 발생 시 자동으로 호출
    private void OnMove(InputValue value)
    {
        MoveInput = value.Get<Vector2>();
    }
}
