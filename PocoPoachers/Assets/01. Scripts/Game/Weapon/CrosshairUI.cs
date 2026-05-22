using System.Collections;
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
    [SerializeField] private float _minSpread = 10f;
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

    [Header("재장전 게이지")]
    [SerializeField] private GameObject _reloadGaugeRoot;
    [SerializeField] private Image _reloadGauge;
    [SerializeField] private CanvasGroup _reloadGaugeGroup;
    [SerializeField] private float _reloadGaugeFadeInDuration = 0.15f;
    [SerializeField] private float _reloadGaugeFadeOutDuration = 0.15f;
    [SerializeField] private float _reloadCrosshairRotationAngle = 45f;
    [SerializeField] private float _reloadCrosshairRotationDuration = 0.2f;
    [SerializeField] private float _reloadCrosshairReturnDuration = 0.15f;

    private Coroutine _reloadGaugeCoroutine;
    private Coroutine _reloadCrosshairRotationCoroutine;
    private float _reloadCrosshairCurrentAngle;
    private float _topBaseAngle;
    private float _bottomBaseAngle;
    private float _leftBaseAngle;
    private float _rightBaseAngle;

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
        CacheCrosshairLineRotations();
        EnsureHitMarker();
        EnsureReloadGaugeGroup();
        if (_hitMarkerGroup != null) _hitMarkerGroup.alpha = 0f;
        if (_reloadGaugeGroup != null) _reloadGaugeGroup.alpha = 0f;
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
        _targetBaseSpread = (isAiming ? gunData.aimSpreadAngle : gunData.spreadAngle) * _pixelsPerDegree + _minSpread;
        _maxSpread = (isAiming ? gunData.aimSpreadAngle : gunData.spreadAngle) * _pixelsPerDegree + _minSpread + _spreadIncrement * 3f;
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
        float radians = _reloadCrosshairCurrentAngle * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        Vector2 top = RotateVector(new Vector2(0f, _currentSpread), sin, cos);
        Vector2 bottom = RotateVector(new Vector2(0f, -_currentSpread), sin, cos);
        Vector2 left = RotateVector(new Vector2(-_currentSpread, 0f), sin, cos);
        Vector2 right = RotateVector(new Vector2(_currentSpread, 0f), sin, cos);

        _top.anchoredPosition = top;
        _bottom.anchoredPosition = bottom;
        _left.anchoredPosition = left;
        _right.anchoredPosition = right;

        ApplyReloadCrosshairLineRotation(_top, _topBaseAngle);
        ApplyReloadCrosshairLineRotation(_bottom, _bottomBaseAngle);
        ApplyReloadCrosshairLineRotation(_left, _leftBaseAngle);
        ApplyReloadCrosshairLineRotation(_right, _rightBaseAngle);
    }

    public void StartReloadGauge(float duration)
    {
        if (_reloadGauge == null || _reloadGaugeRoot == null) return;
        if (_reloadGaugeCoroutine != null) StopCoroutine(_reloadGaugeCoroutine);
        EnsureReloadGaugeGroup();
        StartReloadCrosshairRotation(_reloadCrosshairRotationAngle, _reloadCrosshairRotationDuration);
        _reloadGaugeCoroutine = StartCoroutine(ReloadGaugeRoutine(duration));
    }

    public void StopReloadGauge()
    {
        if (_reloadGaugeRoot == null) return;
        if (_reloadGaugeCoroutine != null)
        {
            StopCoroutine(_reloadGaugeCoroutine);
            _reloadGaugeCoroutine = null;
        }
        if (_reloadGauge != null) _reloadGauge.fillAmount = 0f;
        if (_reloadGaugeGroup != null) _reloadGaugeGroup.alpha = 0f;
        _reloadGaugeRoot.SetActive(false);
        StartReloadCrosshairRotation(0f, _reloadCrosshairReturnDuration);
    }

    private IEnumerator ReloadGaugeRoutine(float duration)
    {
        _reloadGauge.fillAmount = 0f;
        if (_reloadGaugeGroup != null) _reloadGaugeGroup.alpha = 0f;
        _reloadGaugeRoot.SetActive(true);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (_reloadGaugeGroup != null)
            {
                _reloadGaugeGroup.alpha = _reloadGaugeFadeInDuration > 0f
                    ? Mathf.Clamp01(elapsed / _reloadGaugeFadeInDuration)
                    : 1f;
            }
            _reloadGauge.fillAmount = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        _reloadGauge.fillAmount = 1f;
        if (_reloadGaugeGroup != null)
        {
            float fadeElapsed = 0f;
            float startAlpha = _reloadGaugeGroup.alpha;
            while (fadeElapsed < _reloadGaugeFadeOutDuration)
            {
                fadeElapsed += Time.deltaTime;
                _reloadGaugeGroup.alpha = _reloadGaugeFadeOutDuration > 0f
                    ? Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(fadeElapsed / _reloadGaugeFadeOutDuration))
                    : 0f;
                yield return null;
            }

            _reloadGaugeGroup.alpha = 0f;
        }
        _reloadGaugeRoot.SetActive(false);
        _reloadGaugeCoroutine = null;
        StartReloadCrosshairRotation(0f, _reloadCrosshairReturnDuration);
    }

    private Vector2 RotateVector(Vector2 vector, float sin, float cos)
    {
        return new Vector2(
            vector.x * cos - vector.y * sin,
            vector.x * sin + vector.y * cos);
    }

    private void CacheCrosshairLineRotations()
    {
        _topBaseAngle = GetLocalZAngle(_top);
        _bottomBaseAngle = GetLocalZAngle(_bottom);
        _leftBaseAngle = GetLocalZAngle(_left);
        _rightBaseAngle = GetLocalZAngle(_right);
    }

    private float GetLocalZAngle(RectTransform line)
    {
        return line != null ? line.localEulerAngles.z : 0f;
    }

    private void ApplyReloadCrosshairLineRotation(RectTransform line, float baseAngle)
    {
        if (line == null) return;

        Vector3 eulerAngles = line.localEulerAngles;
        eulerAngles.z = baseAngle + _reloadCrosshairCurrentAngle;
        line.localEulerAngles = eulerAngles;
    }

    private void StartReloadCrosshairRotation(float targetAngle, float duration)
    {
        if (_reloadCrosshairRotationCoroutine != null)
        {
            StopCoroutine(_reloadCrosshairRotationCoroutine);
            _reloadCrosshairRotationCoroutine = null;
        }

        if (!isActiveAndEnabled)
        {
            _reloadCrosshairCurrentAngle = targetAngle;
            ApplySpread();
            return;
        }

        _reloadCrosshairRotationCoroutine = StartCoroutine(ReloadCrosshairRotationRoutine(targetAngle, duration));
    }

    private IEnumerator ReloadCrosshairRotationRoutine(float targetAngle, float duration)
    {
        float startAngle = _reloadCrosshairCurrentAngle;

        if (duration <= 0f)
        {
            _reloadCrosshairCurrentAngle = targetAngle;
            _reloadCrosshairRotationCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _reloadCrosshairCurrentAngle = Mathf.Lerp(startAngle, targetAngle, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        _reloadCrosshairCurrentAngle = targetAngle;
        _reloadCrosshairRotationCoroutine = null;
    }

    private void EnsureReloadGaugeGroup()
    {
        if (_reloadGaugeGroup != null || _reloadGaugeRoot == null) return;

        if (!_reloadGaugeRoot.TryGetComponent(out _reloadGaugeGroup))
            _reloadGaugeGroup = _reloadGaugeRoot.AddComponent<CanvasGroup>();
    }

    public void SetGameMode(bool isGameMode)
    {
        gameObject.SetActive(isGameMode);
        Cursor.visible = !isGameMode;
        if (!isGameMode && _hitMarkerGroup != null) _hitMarkerGroup.alpha = 0f;
        //Cursor.lockState = isGameMode ? CursorLockMode.Confined : CursorLockMode.None;
    }
}
