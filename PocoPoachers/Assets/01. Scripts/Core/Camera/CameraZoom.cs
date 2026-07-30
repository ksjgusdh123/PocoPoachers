using UnityEngine;

public class CameraZoom : MonoBehaviour
{
    public static CameraZoom Instance { get; private set; }

    public float DefaultFOV => _defaultFOV;

    [SerializeField] private float _zoomOutTime = 0.3f;

    private Camera _camera;
    private float _defaultFOV;
    private float _targetFOV;
    private float _zoomInStep;
    private float _zoomOutStep;

    private void Awake()
    {
        Instance = this;
        _camera = GetComponent<Camera>();
        _defaultFOV = _camera.fieldOfView;
        _targetFOV = _defaultFOV;
    }

    // 씬이 언로드되면 이 컴포넌트는 파괴되지만 static Instance에는 파괴된 참조가 남는다.
    // 다음 씬에서 새 카메라가 생기기 전에 접근하면 MissingReferenceException이 난다.
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public float ZoomProgress
    {
        get
        {
            float delta = _defaultFOV - _targetFOV;
            if (Mathf.Approximately(delta, 0f)) return 0f;
            return Mathf.Clamp01((_defaultFOV - _camera.fieldOfView) / delta);
        }
    }

    public void SetAiming(bool isAiming, float aimFOV, float aimTime)
    {
        float fovDelta = Mathf.Abs(_defaultFOV - aimFOV);
        _targetFOV = isAiming ? aimFOV : _defaultFOV;
        _zoomInStep = fovDelta / Mathf.Max(aimTime, 0.001f);
        _zoomOutStep = fovDelta / Mathf.Max(_zoomOutTime, 0.001f);
    }

    private void Update()
    {
        if (Mathf.Approximately(_camera.fieldOfView, _targetFOV)) return;

        bool zoomingIn = _camera.fieldOfView > _targetFOV;
        float step = zoomingIn ? _zoomInStep : _zoomOutStep;
        _camera.fieldOfView = Mathf.MoveTowards(_camera.fieldOfView, _targetFOV, step * Time.deltaTime);
    }
}
