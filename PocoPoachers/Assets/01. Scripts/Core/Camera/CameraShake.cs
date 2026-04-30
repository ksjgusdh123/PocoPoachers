using UnityEngine;

public class CameraShake : MonoBehaviour, ICameraEffect
{
    public static CameraShake Instance { get; private set; }

    public Vector3 PositionOffset { get; private set; }

    private float _intensity;
    private float _remaining;

    private void Awake()
    {
        Instance = this;
    }

    private Vector3 _direction;

    public void Shake(float intensity, float duration, Vector3 direction)
    {
        _intensity = intensity;
        _remaining = duration;
        _duration = duration;
        _direction = direction.normalized;
    }

    private float _duration;

    private void Update()
    {
        if (_remaining <= 0f)
        {
            PositionOffset = Vector3.zero;
            return;
        }

        _remaining -= Time.deltaTime;
        float t = _remaining / _duration;
        PositionOffset = _direction * (_intensity * t);
    }
}
