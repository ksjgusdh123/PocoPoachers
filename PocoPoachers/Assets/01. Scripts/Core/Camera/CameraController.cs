using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _baseOffset = new Vector3(0f, 10f, -7f);
    [SerializeField] private float _smoothTime = 0.1f;
    [SerializeField] private float _spectateSmoothTime = 0.35f; // 관전 시 스무딩(원격 보간 떨림 완화용, 클수록 부드럽고 지연↑)

    private ICameraEffect[] _effects;
    private Vector3 _velocity;
    private bool _isLocked;
    private bool _followPosition = true;
    private float _defaultSmoothTime;

    public void SetTarget(Transform target) => _target = target;

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
        if (_target == null || !_followPosition) return;

        Vector3 targetPos = _target.position + _baseOffset;

        if (!_isLocked)
        {
            foreach (var effect in _effects)
                targetPos += effect.PositionOffset;
        }

        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, _smoothTime);
    }
}
