using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private Vector3 _baseOffset = new Vector3(0f, 10f, -7f);
    [SerializeField] private float _smoothTime = 0.1f;

    private ICameraEffect[] _effects;
    private Vector3 _velocity;

    private void Awake()
    {
        _effects = GetComponents<ICameraEffect>();
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        Vector3 targetPos = _target.position + _baseOffset;

        foreach (var effect in _effects)
            targetPos += effect.PositionOffset;

        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, _smoothTime);
    }
}
