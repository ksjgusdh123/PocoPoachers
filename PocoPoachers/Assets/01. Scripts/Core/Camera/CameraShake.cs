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

    // 씬 언로드로 파괴된 뒤에도 static Instance가 파괴된 참조를 가리키면
    // 다음 씬에서 Shake 호출 시 MissingReferenceException이 난다.
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
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
