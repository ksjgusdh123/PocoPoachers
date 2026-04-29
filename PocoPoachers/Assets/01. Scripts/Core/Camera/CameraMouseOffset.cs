using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMouseOffset : MonoBehaviour, ICameraEffect
{
    [SerializeField] private float _maxOffset = 3f;
    [SerializeField] private float _smoothTime = 0.15f;

    public Vector3 PositionOffset => _currentOffset;

    private Vector3 _currentOffset;
    private Vector3 _velocity;

    private void Update()
    {
        Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
        Vector2 viewportPos = Camera.main.ScreenToViewportPoint(mouseScreenPos);

        // 뷰포트 중심(0.5, 0.5) 기준 -1 ~ 1 범위로 정규화
        Vector2 centerOffset = (viewportPos - new Vector2(0.5f, 0.5f)) * 2f;

        // 카메라의 XZ 평면 방향 기준으로 오프셋 계산
        Vector3 camRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 camForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;

        Vector3 targetOffset = (camRight * centerOffset.x + camForward * centerOffset.y) * _maxOffset;

        _currentOffset = Vector3.SmoothDamp(_currentOffset, targetOffset, ref _velocity, _smoothTime);
    }
}
