using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerRotation : MonoBehaviour
{
    [SerializeField] private float _runRotationSpeed = 10f;

    private static readonly Plane GroundPlane = new Plane(Vector3.up, Vector3.zero);
    private PlayerInputHandler _inputHandler;

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
    }

    private void Update()
    {
        if (_inputHandler.IsSprintPressed && _inputHandler.MoveInput.sqrMagnitude > 0.01f)
            RotateTowardMovement();
        else
            RotateTowardMouse();
    }

    private void RotateTowardMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (!GroundPlane.Raycast(ray, out float distance)) return;

        Vector3 hitPoint = ray.GetPoint(distance);
        Vector3 direction = hitPoint - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f) return;

        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void RotateTowardMovement()
    {
        Vector2 input = _inputHandler.MoveInput;
        Vector3 moveDir = new Vector3(input.x, 0f, input.y);

        Quaternion target = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, _runRotationSpeed * Time.deltaTime);
    }
}
