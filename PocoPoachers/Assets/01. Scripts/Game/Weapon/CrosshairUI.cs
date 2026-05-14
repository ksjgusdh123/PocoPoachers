using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    public static CrosshairUI Instance { get; private set; }
    public Vector2 ScreenPosition { get; private set; }

    [SerializeField] private RectTransform _top, _bottom, _left, _right;

    [Header("에임 점")]
    [SerializeField] private Image _dot;
    [SerializeField] private float _dotThreshold = 0.75f;
    [SerializeField] private float _dotFadeSpeed = 10f;

    [Header("스프레드 설정")]
    [SerializeField] private float _pixelsPerDegree = 5f;
    [SerializeField] private float _spreadIncrement = 20f;
    [SerializeField] private float _recoverySpeed = 80f;
    [SerializeField] private float _collapseSpeed = 200f;
    [SerializeField] private float _collapseTargetSpread = 0f;

    private float _maxSpread;

    private RectTransform _rectTransform;
    private float _currentSpread;
    private float _targetBaseSpread;
    private bool _isCollapsing;
    private bool _isSwitchExpanding;

    [Header("크로스헤어 반동")]
    [SerializeField] private float _kickSpeed = 300f;
    [SerializeField] private float _kickRecovery = 150f;

    [Header("Hit Marker")]
    [SerializeField] private CanvasGroup _hitMarkerGroup;
    [SerializeField] private Color _hitMarkerColor = Color.white;
    [SerializeField] private float _hitMarkerDuration = 0.12f;
    [SerializeField] private float _hitMarkerFadeSpeed = 20f;
    [SerializeField] private float _hitMarkerSpreadScale = 0.7f;
    [SerializeField] private float _hitMarkerDistanceOffset = 8f;
    [SerializeField] private float _hitMarkerConvergeTime = 0.04f;
    [SerializeField] private RectTransform _hitMarkerTopLeft;
    [SerializeField] private RectTransform _hitMarkerTopRight;
    [SerializeField] private RectTransform _hitMarkerBottomLeft;
    [SerializeField] private RectTransform _hitMarkerBottomRight;

    private Vector2 _recoilTarget;
    private Vector2 _recoilOffset;
    private Vector2 _lastMousePosition;
    private float _shakeIntensity;
    private float _shakeDuration;
    private float _shakeTimer;
    private float _shakeAngle;
    private float _hitMarkerTimer;
    private float _activeHitMarkerDistance;
    private float _activeHitMarkerOuterDistance;
    private RectTransform[] _hitMarkerLines;
    private bool _hasLastMousePosition;
    private bool _ignoreWarpDelta;

    private void Awake()
    {
        Instance = this;
        _rectTransform = GetComponent<RectTransform>();
        EnsureHitMarker();
        if (_hitMarkerGroup != null) _hitMarkerGroup.alpha = 0f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void Update()
    {
        _recoilTarget = Vector2.MoveTowards(_recoilTarget, Vector2.zero, _kickRecovery * Time.deltaTime);
        _recoilOffset = Vector2.MoveTowards(_recoilOffset, _recoilTarget, _kickSpeed * Time.deltaTime);

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 mouseDelta = _hasLastMousePosition ? mousePos - _lastMousePosition : Vector2.zero;
        bool movedByPlayer = !_ignoreWarpDelta && mouseDelta.sqrMagnitude > 0.01f;
        _ignoreWarpDelta = false;

        Vector2 targetPos = new Vector2(
            Mathf.Clamp(mousePos.x + _recoilTarget.x, 0f, Screen.width),
            Mathf.Clamp(mousePos.y + _recoilTarget.y, 0f, Screen.height));
        _recoilTarget = targetPos - mousePos;

        Vector2 crosshairPos = new Vector2(
            Mathf.Clamp(mousePos.x + _recoilOffset.x, 0f, Screen.width),
            Mathf.Clamp(mousePos.y + _recoilOffset.y, 0f, Screen.height));
        _recoilOffset = crosshairPos - mousePos;

        if (movedByPlayer && _recoilOffset.sqrMagnitude > 0.01f)
        {
            Mouse.current.WarpCursorPosition(crosshairPos);
            mousePos = crosshairPos;
            _recoilOffset = Vector2.zero;
            _recoilTarget = Vector2.zero;
            _ignoreWarpDelta = true;
        }

        _lastMousePosition = mousePos;
        _hasLastMousePosition = true;

        ScreenPosition = crosshairPos;
        _rectTransform.position = crosshairPos;

        _shakeTimer = Mathf.Max(_shakeTimer - Time.deltaTime, 0f);
        float shakeRotation = _shakeTimer > 0f ? _shakeAngle * (_shakeTimer / _shakeDuration) : 0f;
        _rectTransform.localEulerAngles = new Vector3(0f, 0f, shakeRotation);

        if (_isCollapsing)
        {
            _currentSpread = Mathf.MoveTowards(_currentSpread, _collapseTargetSpread, _collapseSpeed * Time.deltaTime);
            if (_currentSpread <= _collapseTargetSpread)
            {
                _isCollapsing = false;
                _isSwitchExpanding = true;
            }
        }
        else if (_isSwitchExpanding)
        {
            _currentSpread = Mathf.MoveTowards(_currentSpread, _targetBaseSpread, _collapseSpeed * Time.deltaTime);
            if (Mathf.Approximately(_currentSpread, _targetBaseSpread)) _isSwitchExpanding = false;
        }
        else
        {
            _currentSpread = Mathf.MoveTowards(_currentSpread, _targetBaseSpread, _recoverySpeed * Time.deltaTime);
        }

        ApplySpread();
        UpdateDot();
        UpdateHitMarker();
    }

    private void UpdateDot()
    {
        if (_dot == null) return;
        float zoom = CameraZoom.Instance?.ZoomProgress ?? 0f;
        float targetAlpha = zoom >= _dotThreshold ? 1f : 0f;
        Color c = _dot.color;
        c.a = Mathf.Lerp(c.a, targetAlpha, _dotFadeSpeed * Time.deltaTime);
        _dot.color = c;
    }

    public void UpdateBaseSpread(GunData gunData, bool isAiming)
    {
        if (gunData == null) return;
        _targetBaseSpread = (isAiming ? gunData.aimSpreadAngle : gunData.spreadAngle) * _pixelsPerDegree;
        _maxSpread = (isAiming ? gunData.aimSpreadAngle : gunData.spreadAngle) * _pixelsPerDegree + _spreadIncrement * 3f;
        _shakeIntensity = gunData.crosshairShakeIntensity;
        _shakeDuration = gunData.crosshairShakeDuration;
    }

    public void ResetSpread()
    {
        _isCollapsing = true;
    }

    public void OnShoot(Vector2 kickVector)
    {
        _currentSpread = Mathf.Min(_currentSpread + _spreadIncrement, _maxSpread);
        _recoilTarget += kickVector;
        _shakeAngle = UnityEngine.Random.Range(-_shakeIntensity, _shakeIntensity);
        _shakeTimer = _shakeDuration;
    }

    public void ShowHitMarker()
    {
        EnsureHitMarker();
        if (_hitMarkerGroup == null) return;

        _hitMarkerTimer = _hitMarkerDuration;
        _hitMarkerGroup.alpha = 1f;
        UpdateActiveHitMarkerDistances();
        ApplyHitMarkerDistance(_activeHitMarkerOuterDistance);
    }

    private void UpdateHitMarker()
    {
        if (_hitMarkerGroup == null) return;

        if (_hitMarkerTimer > 0f)
        {
            _hitMarkerTimer -= Time.deltaTime;
            UpdateHitMarkerDistance();
            return;
        }

        ApplyHitMarkerDistance(_activeHitMarkerOuterDistance);
        _hitMarkerGroup.alpha = Mathf.MoveTowards(_hitMarkerGroup.alpha, 0f, _hitMarkerFadeSpeed * Time.deltaTime);
    }

    private void UpdateActiveHitMarkerDistances()
    {
        _activeHitMarkerDistance = Mathf.Max(_currentSpread, _targetBaseSpread, 0f) * _hitMarkerSpreadScale;
        _activeHitMarkerOuterDistance = Mathf.Max(_activeHitMarkerDistance + _hitMarkerDistanceOffset, 0f);
    }

    private void UpdateHitMarkerDistance()
    {
        if (_hitMarkerDuration <= 0f)
        {
            ApplyHitMarkerDistance(_activeHitMarkerDistance);
            return;
        }

        float elapsed = _hitMarkerDuration - _hitMarkerTimer;
        float distance;

        if (elapsed < _hitMarkerConvergeTime)
        {
            float t = _hitMarkerConvergeTime > 0f ? elapsed / _hitMarkerConvergeTime : 1f;
            distance = Mathf.Lerp(_activeHitMarkerOuterDistance, _activeHitMarkerDistance, t);
            _hitMarkerGroup.alpha = 1f;
        }
        else
        {
            float expandDuration = Mathf.Max(_hitMarkerDuration - _hitMarkerConvergeTime, 0.001f);
            float t = Mathf.Clamp01((elapsed - _hitMarkerConvergeTime) / expandDuration);
            distance = Mathf.Lerp(_activeHitMarkerDistance, _activeHitMarkerOuterDistance, t);
            _hitMarkerGroup.alpha = Mathf.Lerp(1f, 0f, t);
        }

        ApplyHitMarkerDistance(distance);
    }

    private void ApplyHitMarkerDistance(float distance)
    {
        if (_hitMarkerLines == null || _hitMarkerLines.Length < 4) return;

        if (_hitMarkerLines[0] == null || _hitMarkerLines[1] == null || _hitMarkerLines[2] == null || _hitMarkerLines[3] == null) return;

        _hitMarkerLines[0].anchoredPosition = new Vector2(-distance, distance);
        _hitMarkerLines[1].anchoredPosition = new Vector2(distance, distance);
        _hitMarkerLines[2].anchoredPosition = new Vector2(-distance, -distance);
        _hitMarkerLines[3].anchoredPosition = new Vector2(distance, -distance);
    }

    private void EnsureHitMarker()
    {
        if (_hitMarkerGroup == null) return;

        if (!HasHitMarkerLines()) CacheHitMarkerLines(_hitMarkerGroup.transform);
    }

    private void CacheHitMarkerLines(Transform hitMarker)
    {
        _hitMarkerLines = new RectTransform[4];
        _hitMarkerLines[0] = _hitMarkerTopLeft != null ? _hitMarkerTopLeft : FindHitMarkerLine(hitMarker, "TopLeft");
        _hitMarkerLines[1] = _hitMarkerTopRight != null ? _hitMarkerTopRight : FindHitMarkerLine(hitMarker, "TopRight");
        _hitMarkerLines[2] = _hitMarkerBottomLeft != null ? _hitMarkerBottomLeft : FindHitMarkerLine(hitMarker, "BottomLeft");
        _hitMarkerLines[3] = _hitMarkerBottomRight != null ? _hitMarkerBottomRight : FindHitMarkerLine(hitMarker, "BottomRight");
        ApplyHitMarkerLineRotations();
        ApplyHitMarkerColor();
    }

    private void ApplyHitMarkerLineRotations()
    {
        if (!HasHitMarkerLines()) return;

        _hitMarkerLines[0].localEulerAngles = new Vector3(0f, 0f, -45f);
        _hitMarkerLines[1].localEulerAngles = new Vector3(0f, 0f, 45f);
        _hitMarkerLines[2].localEulerAngles = new Vector3(0f, 0f, 45f);
        _hitMarkerLines[3].localEulerAngles = new Vector3(0f, 0f, -45f);
    }

    private void ApplyHitMarkerColor()
    {
        if (!HasHitMarkerLines()) return;

        for (int i = 0; i < _hitMarkerLines.Length; i++)
        {
            if (_hitMarkerLines[i].TryGetComponent<Image>(out var image))
            {
                image.color = _hitMarkerColor;
                image.raycastTarget = false;
            }
        }
    }

    private RectTransform FindHitMarkerLine(Transform root, string lineName)
    {
        if (root == null) return null;

        foreach (RectTransform rectTransform in root.GetComponentsInChildren<RectTransform>(true))
        {
            if (rectTransform.name == lineName) return rectTransform;
        }

        return null;
    }

    private bool HasHitMarkerLines()
    {
        return _hitMarkerLines != null
            && _hitMarkerLines.Length >= 4
            && _hitMarkerLines[0] != null
            && _hitMarkerLines[1] != null
            && _hitMarkerLines[2] != null
            && _hitMarkerLines[3] != null;
    }

    private void ApplySpread()
    {
        _top.anchoredPosition    = new Vector2(0,  _currentSpread);
        _bottom.anchoredPosition = new Vector2(0, -_currentSpread);
        _left.anchoredPosition   = new Vector2(-_currentSpread, 0);
        _right.anchoredPosition  = new Vector2( _currentSpread, 0);
    }

    public void SetGameMode(bool isGameMode)
    {
        gameObject.SetActive(isGameMode);
        Cursor.visible = !isGameMode;
        if (!isGameMode && _hitMarkerGroup != null) _hitMarkerGroup.alpha = 0f;
        //Cursor.lockState = isGameMode ? CursorLockMode.Confined : CursorLockMode.None;
    }
}
