using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    public static CameraZoom Instance { get; private set; }

    [SerializeField] private float _zoomInSpeed = 10f;
    [SerializeField] private float _zoomOutSpeed = 5f;

    private Camera _camera;
    private float _defaultFOV;
    private float _targetFOV;

    private void Awake()
    {
        Instance = this;
        _camera = GetComponent<Camera>();
        _defaultFOV = _camera.fieldOfView;
        _targetFOV = _defaultFOV;
    }

    public void SetAiming(bool isAiming, float aimFOV)
    {
        _targetFOV = isAiming ? aimFOV : _defaultFOV;
    }

    private void Update()
    {
        float speed = _camera.fieldOfView > _targetFOV ? _zoomInSpeed : _zoomOutSpeed;
        _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, _targetFOV, speed * Time.deltaTime);
    }
}
