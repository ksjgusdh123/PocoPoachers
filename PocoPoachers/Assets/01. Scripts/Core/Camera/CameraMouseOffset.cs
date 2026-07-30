using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMouseOffset : MonoBehaviour, ICameraEffect
{
    [SerializeField] private float _maxOffset = 3f;
    [SerializeField] private float _smoothTime = 0.15f;

    public Vector3 PositionOffset => _currentOffset;

    private Vector3 _currentOffset;
    private Vector3 _velocity;
    private Camera _camera;

    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        // 마우스가 없는 입력 환경(게임패드/터치 전용)에서는 Mouse.current가 null이다.
        if (Mouse.current == null) return;

        // Camera.main은 태그 탐색이므로 매 프레임 호출을 피하고, 씬 전환으로 교체되면 다시 잡는다.
        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;

        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector2 viewportPos = _camera.ScreenToViewportPoint(mouseScreenPos);

        // 뷰포트 중심(0.5, 0.5) 기준 -1 ~ 1 범위로 정규화
        Vector2 centerOffset = (viewportPos - new Vector2(0.5f, 0.5f)) * 2f;

        // 카메라의 XZ 평면 방향 기준으로 오프셋 계산
        Vector3 camRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 camForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        Vector3 targetOffset = (camRight * centerOffset.x + camForward * centerOffset.y) * _maxOffset;

        _currentOffset = Vector3.SmoothDamp(_currentOffset, targetOffset, ref _velocity, _smoothTime);
    }
}
