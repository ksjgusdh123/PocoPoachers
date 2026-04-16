using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _moveSpeed = 5f;

    private CharacterController _characterController;
    private PlayerInputHandler _inputHandler;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        Move();
    }

    private void Move()
    {
        Vector2 input = _inputHandler.MoveInput;
        Vector3 moveDir = new Vector3(input.x, 0f, input.y);
        _characterController.Move(moveDir * _moveSpeed * Time.deltaTime);
    }
}
