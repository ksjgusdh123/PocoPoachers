using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerRotation : MonoBehaviour
{
    [SerializeField] private float _runRotationSpeed = 10f;
    [SerializeField] private float _mouseRotationSpeed = 15f;
    [SerializeField] private float _recoveryRotationSpeed = 3f;

    private static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);
    private PlayerInputHandler _inputHandler;
    private PlayerDodge _playerDodge;

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
        _playerDodge = GetComponent<PlayerDodge>();
    }

    private void Update()
    {
        if (_playerDodge.IsRolling) return;

        if (_inputHandler.IsSprintPressed && _inputHandler.MoveInput.sqrMagnitude > 0.01f)
            RotateTowardMovement();
        else
            RotateTowardMouse();
    }

    private void RotateTowardMouse()
    {
        Vector2 screenPos = CrosshairUI.Instance != null
            ? CrosshairUI.Instance.ScreenPosition
            : Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(screenPos);

        if (!GroundPlane.Raycast(ray, out float distance)) return;

        Vector3 hitPoint = ray.GetPoint(distance);
        Vector3 direction = hitPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        float speed = _playerDodge.IsRecovering ? _recoveryRotationSpeed : _mouseRotationSpeed;
        Quaternion target = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, speed * Time.deltaTime);
    }

    private void RotateTowardMovement()
    {
        Vector2 input = _inputHandler.MoveInput;
        Vector3 moveDir = new Vector3(input.x, 0f, input.y);

        Quaternion target = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, _runRotationSpeed * Time.deltaTime);
    }
}
