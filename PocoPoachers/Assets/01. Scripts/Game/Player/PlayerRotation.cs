using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerRotation : MonoBehaviour
{
    [SerializeField] private float _runRotationSpeed = 10f;
    [SerializeField] private float _mouseRotationSpeed = 25f;
    [SerializeField] private float _recoveryRotationSpeed = 3f;

    private PlayerInputHandler _inputHandler;
    private PlayerDodge _playerDodge;
    private WeaponController _weaponController;

    private void Awake()
    {
        _inputHandler = GetComponent<PlayerInputHandler>();
        _playerDodge = GetComponent<PlayerDodge>();
        _weaponController = GetComponent<WeaponController>();
    }

    private void Update()
    {
        // 마우스 회전은 액션 맵이 아니라 커서 위치를 직접 읽으므로, 맵을 꺼도 연출 중에 몸이 계속 돈다
        if (_inputHandler.IsInputLocked) return;
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

        // 총 발사 방향(WeaponController.GetCrosshairAimDirection)과 기준 높이를 맞춘다 —
        // 몸 회전은 Y=0 고정 평면, 사격 방향은 총구 높이 평면을 쓰면 둘이 어긋나 보일 수 있다.
        Transform muzzle = _weaponController != null ? _weaponController.CurrentGun?.Muzzle : null;
        float planeHeight = muzzle != null ? muzzle.position.y : transform.position.y;
        Plane aimPlane = new Plane(Vector3.up, new Vector3(0f, planeHeight, 0f));

        if (!aimPlane.Raycast(ray, out float distance)) return;

        Vector3 hitPoint = ray.GetPoint(distance);
        Vector3 direction = hitPoint - transform.position;
        direction.y = 0f;

        // 카메라가 높은 각도로 내려다보기 때문에 크로스헤어가 플레이어 바로 밑 화면 영역에 있으면
        // 지면 교차점이 플레이어 위치 근처의 아주 좁은 반경으로 수렴한다. 데드존이 넓으면 이 구간에서
        // 회전 갱신이 통째로 스킵되어 직전 방향(예: 위쪽)이 그대로 얼어붙어 버린다.
        if (direction.sqrMagnitude < 0.0001f) return;

        float speed = _playerDodge.IsRecovering ? _recoveryRotationSpeed : _mouseRotationSpeed;
        Quaternion target = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, speed * Time.deltaTime);
    }

    private void RotateTowardMovement()
    {
        Vector2 input = _inputHandler.MoveInput;
        Vector3 moveDir = CameraSpace.InputToWorld(input);

        Quaternion target = Quaternion.LookRotation(moveDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, _runRotationSpeed * Time.deltaTime);
    }
}
