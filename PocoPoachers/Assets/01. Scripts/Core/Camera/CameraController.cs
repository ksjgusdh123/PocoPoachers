using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _baseOffset = new Vector3(0f, 10f, -7f);
    [SerializeField] private float _smoothTime = 0.1f;
    [SerializeField] private float _spectateSmoothTime = 0.35f; // 관전 시 스무딩(원격 보간 떨림 완화용, 클수록 부드럽고 지연↑)

    [SerializeField] private float _focusSmoothTime = 0.6f; // 연출로 다른 지점을 비추러 갔다 돌아올 때의 스무딩

    private ICameraEffect[] _effects;
    private Vector3 _velocity;
    private bool _isLocked;
    private bool _followPosition = true;
    private float _defaultSmoothTime;
    private Vector3? _focusPoint;
    private bool _returningFromFocus;

    public void SetTarget(Transform target) => _target = target;

    // 연출용 — 대상 대신 지정한 지점을 비춘다. null을 넘기면 천천히 대상에게 돌아간다.
    public void SetFocusPoint(Vector3? point)
    {
        if (_focusPoint.HasValue && !point.HasValue) _returningFromFocus = true;
        _focusPoint = point;
    }

    public void SetLocked(bool locked)
    {
        _isLocked = locked;
    }

    // 대상 위치 추적을 잠시 끈다 — 대상이 연출 등으로 순간이동/상승할 때 카메라가 같이 따라가지 않도록
    public void SetFollowPosition(bool follow)
    {
        _followPosition = follow;
    }

    // 관전 모드 — 이펙트(마우스 오프셋/셰이크)를 끄고 스무딩을 강화해 원격 플레이어의 네트워크 보간 떨림을 완화한다
    public void SetSpectate(bool on)
    {
        _isLocked = on;
        _smoothTime = on ? _spectateSmoothTime : _defaultSmoothTime;
    }

    private void Awake()
    {
        _effects = GetComponents<ICameraEffect>();
        _defaultSmoothTime = _smoothTime;
    }

    private void LateUpdate()
    {
        if (_focusPoint.HasValue)
        {
            transform.position = Vector3.SmoothDamp(transform.position, _focusPoint.Value + _baseOffset, ref _velocity, _focusSmoothTime);
            return;
        }

        if (_target == null || !_followPosition) return;

        Vector3 targetPos = _target.position + _baseOffset;

        if (!_isLocked)
        {
            foreach (var effect in _effects)
                targetPos += effect.PositionOffset;
        }

        // 기본 스무딩은 짧아서 연출 지점에서 돌아올 때 순간이동처럼 보인다. 거의 도착할 때까지 느리게 따라간다.
        if (_returningFromFocus && (transform.position - targetPos).sqrMagnitude < 0.04f) _returningFromFocus = false;
        float smoothTime = _returningFromFocus ? _focusSmoothTime : _smoothTime;

        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, smoothTime);
    }
}
