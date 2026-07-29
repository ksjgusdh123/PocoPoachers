using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
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
    [SerializeField] private float _reloadGaugeFadeDuration = 0.15f;
    [SerializeField] private float _reloadCrosshairRotationAngle = 45f;
    [SerializeField] private float _reloadCrosshairRotationDuration = 0.2f;
    [SerializeField] private float _reloadCrosshairMinSpread = 35f;
    [SerializeField] private bool _matchCrosshairLineSizeOnReload = true;
    [SerializeField] private Color _reloadCrosshairBaseColor = new Color(0.55f, 0.55f, 0.55f, 1f);
    [SerializeField] private Color _reloadCrosshairPulseColor = Color.white;
    [SerializeField] private float _reloadCrosshairPulseInterval = 0.18f;
    [SerializeField] private float _reloadCrosshairPulseScale = 1.25f;

    private Coroutine _reloadGaugeCoroutine;
    private Coroutine _reloadCrosshairRotationCoroutine;
    private float _reloadCrosshairCurrentAngle;
    private float _reloadCrosshairSizeProgress;
    private float _topBaseAngle;
    private float _bottomBaseAngle;
    private float _leftBaseAngle;
    private float _rightBaseAngle;
    private Vector2 _topBaseSize;
    private Vector2 _bottomBaseSize;
    private Vector2 _leftBaseSize;
    private Vector2 _rightBaseSize;
    private Image _topImage;
    private Image _bottomImage;
    private Image _leftImage;
    private Image _rightImage;
    private Color _topBaseColor;
    private Color _bottomBaseColor;
    private Color _leftBaseColor;
    private Color _rightBaseColor;
    private float _topPulseProgress;
    private float _bottomPulseProgress;
    private float _leftPulseProgress;
    private float _rightPulseProgress;

    // ApplySpread가 실제로 반영한 마지막 값 — 변화가 없는 프레임의 레이아웃 갱신을 건너뛰는 데 쓴다.
    private bool _hasAppliedSpread;
    private float _appliedSpread;
    private float _appliedAngle;
    private float _appliedSizeProgress;
    private float _appliedTopPulse;
    private float _appliedRightPulse;
    private float _appliedBottomPulse;
    private float _appliedLeftPulse;

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
        // 쉘터(전투 없는 씬)에서는 크로스헤어를 띄우지 않는다. 이동해 온 씬 이름으로 자동 판별하므로
        // 씬마다 수동 설정이 필요 없다. Instance를 남기지 않아 모든 참조가 마우스 폴백으로 동작하고,
        // 오브젝트를 꺼 일반 커서를 유지한다. Instance가 null이라 UIManager.RefreshCursor도 되살리지 않는다.
        if (SceneName.IsShelter(SceneManager.GetActiveScene().name))
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            gameObject.SetActive(false);
            return;
        }

        Instance = this;
        _rectTransform = GetComponent<RectTransform>();
        CacheCrosshairLineRotations();
        CacheCrosshairLineSizes();
        CacheCrosshairLineImages();
        EnsureHitMarker();
        EnsureReloadGaugeGroup();
        if (_hitMarkerGroup != null) _hitMarkerGroup.alpha = 0f;
        if (_reloadGaugeGroup != null) _reloadGaugeGroup.alpha = 0f;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

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

    public void UpdateBaseSpread(GunStatData stat, bool isAiming)
    {
        if (stat == null) return;
        _targetBaseSpread = (isAiming ? stat.AimSpread : stat.Spread) * _pixelsPerDegree + _minSpread;
        _maxSpread = (isAiming ? stat.AimSpread : stat.Spread) * _pixelsPerDegree + _minSpread + _spreadIncrement * 3f;
        _shakeIntensity = stat.CrosshairShakeIntensity;
        _shakeDuration = stat.CrosshairShakeDuration;
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
        float spread = Mathf.Lerp(
            _currentSpread,
            Mathf.Max(_currentSpread, _reloadCrosshairMinSpread),
            _reloadCrosshairSizeProgress);

        // 매 프레임 8회 SetSizeWithCurrentAnchors를 호출하면 값이 그대로여도 레이아웃이 갱신된다.
        // 스프레드/회전/펄스가 실제로 바뀐 프레임에만 반영한다.
        if (!IsSpreadDirty(spread)) return;
        CacheAppliedSpread(spread);

        float radians = _reloadCrosshairCurrentAngle * Mathf.Deg2Rad;
        float sin = Mathf.Sin(radians);
        float cos = Mathf.Cos(radians);

        Vector2 top = RotateVector(new Vector2(0f, spread), sin, cos);
        Vector2 bottom = RotateVector(new Vector2(0f, -spread), sin, cos);
        Vector2 left = RotateVector(new Vector2(-spread, 0f), sin, cos);
        Vector2 right = RotateVector(new Vector2(spread, 0f), sin, cos);

        _top.anchoredPosition = top;
        _bottom.anchoredPosition = bottom;
        _left.anchoredPosition = left;
        _right.anchoredPosition = right;

        ApplyReloadCrosshairLineRotation(_top, _topBaseAngle);
        ApplyReloadCrosshairLineRotation(_bottom, _bottomBaseAngle);
        ApplyReloadCrosshairLineRotation(_left, _leftBaseAngle);
        ApplyReloadCrosshairLineRotation(_right, _rightBaseAngle);

        ApplyReloadCrosshairLineSize(_top, _topBaseSize, _topPulseProgress);
        ApplyReloadCrosshairLineSize(_bottom, _bottomBaseSize, _bottomPulseProgress);
        ApplyReloadCrosshairLineSize(_left, _leftBaseSize, _leftPulseProgress);
        ApplyReloadCrosshairLineSize(_right, _rightBaseSize, _rightPulseProgress);
    }

    private bool IsSpreadDirty(float spread)
    {
        return !_hasAppliedSpread
            || !Mathf.Approximately(spread, _appliedSpread)
            || !Mathf.Approximately(_reloadCrosshairCurrentAngle, _appliedAngle)
            || !Mathf.Approximately(_reloadCrosshairSizeProgress, _appliedSizeProgress)
            || !Mathf.Approximately(_topPulseProgress, _appliedTopPulse)
            || !Mathf.Approximately(_rightPulseProgress, _appliedRightPulse)
            || !Mathf.Approximately(_bottomPulseProgress, _appliedBottomPulse)
            || !Mathf.Approximately(_leftPulseProgress, _appliedLeftPulse);
    }

    private void CacheAppliedSpread(float spread)
    {
        _hasAppliedSpread = true;
        _appliedSpread = spread;
        _appliedAngle = _reloadCrosshairCurrentAngle;
        _appliedSizeProgress = _reloadCrosshairSizeProgress;
        _appliedTopPulse = _topPulseProgress;
        _appliedRightPulse = _rightPulseProgress;
        _appliedBottomPulse = _bottomPulseProgress;
        _appliedLeftPulse = _leftPulseProgress;
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
        RestoreCrosshairLineColors();
        ResetReloadCrosshairPulse();
        StartReloadCrosshairRotation(0f, _reloadCrosshairRotationDuration);
    }

    private IEnumerator ReloadGaugeRoutine(float duration)
    {
        _reloadGauge.fillAmount = 0f;
        if (_reloadGaugeGroup != null) _reloadGaugeGroup.alpha = 0f;
        _reloadGaugeRoot.SetActive(true);
        ApplyReloadCrosshairBaseColor();

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (_reloadGaugeGroup != null)
            {
                _reloadGaugeGroup.alpha = _reloadGaugeFadeDuration > 0f
                    ? Mathf.Clamp01(elapsed / _reloadGaugeFadeDuration)
                    : 1f;
            }
            _reloadGauge.fillAmount = Mathf.Clamp01(elapsed / duration);
            UpdateReloadCrosshairPulse(elapsed);
            ApplySpread();
            yield return null;
        }

        _reloadGauge.fillAmount = 1f;
        ApplyReloadCrosshairBaseColor();
        ResetReloadCrosshairPulse();
        if (_reloadGaugeGroup != null)
        {
            float fadeElapsed = 0f;
            float startAlpha = _reloadGaugeGroup.alpha;
            while (fadeElapsed < _reloadGaugeFadeDuration)
            {
                fadeElapsed += Time.deltaTime;
                _reloadGaugeGroup.alpha = _reloadGaugeFadeDuration > 0f
                    ? Mathf.Lerp(startAlpha, 0f, Mathf.Clamp01(fadeElapsed / _reloadGaugeFadeDuration))
                    : 0f;
                yield return null;
            }

            _reloadGaugeGroup.alpha = 0f;
        }
        _reloadGaugeRoot.SetActive(false);
        _reloadGaugeCoroutine = null;
        RestoreCrosshairLineColors();
        ResetReloadCrosshairPulse();
        StartReloadCrosshairRotation(0f, _reloadCrosshairRotationDuration);
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

    private void CacheCrosshairLineSizes()
    {
        _topBaseSize = GetRectSize(_top);
        _bottomBaseSize = GetRectSize(_bottom);
        _leftBaseSize = GetRectSize(_left);
        _rightBaseSize = GetRectSize(_right);
    }

    private void CacheCrosshairLineImages()
    {
        _topImage = GetLineImage(_top);
        _bottomImage = GetLineImage(_bottom);
        _leftImage = GetLineImage(_left);
        _rightImage = GetLineImage(_right);

        _topBaseColor = GetImageColor(_topImage);
        _bottomBaseColor = GetImageColor(_bottomImage);
        _leftBaseColor = GetImageColor(_leftImage);
        _rightBaseColor = GetImageColor(_rightImage);
    }

    private float GetLocalZAngle(RectTransform line)
    {
        return line != null ? line.localEulerAngles.z : 0f;
    }

    private Vector2 GetRectSize(RectTransform line)
    {
        return line != null ? line.rect.size : Vector2.zero;
    }

    private Image GetLineImage(RectTransform line)
    {
        return line != null ? line.GetComponent<Image>() : null;
    }

    private Color GetImageColor(Image image)
    {
        return image != null ? image.color : Color.white;
    }

    private void UpdateReloadCrosshairPulse(float elapsed)
    {
        float interval = Mathf.Max(_reloadCrosshairPulseInterval, 0.001f);
        float cyclePosition = Mathf.Repeat(elapsed / interval, 4f);
        int activeIndex = Mathf.FloorToInt(cyclePosition);
        float pulse = Mathf.Sin((cyclePosition - activeIndex) * Mathf.PI);

        _topPulseProgress = activeIndex == 0 ? pulse : 0f;
        _rightPulseProgress = activeIndex == 1 ? pulse : 0f;
        _bottomPulseProgress = activeIndex == 2 ? pulse : 0f;
        _leftPulseProgress = activeIndex == 3 ? pulse : 0f;

        ApplyReloadCrosshairLineColor(_topImage, _topPulseProgress);
        ApplyReloadCrosshairLineColor(_rightImage, _rightPulseProgress);
        ApplyReloadCrosshairLineColor(_bottomImage, _bottomPulseProgress);
        ApplyReloadCrosshairLineColor(_leftImage, _leftPulseProgress);
    }

    private void ApplyReloadCrosshairBaseColor()
    {
        ApplyReloadCrosshairLineColor(_topImage, 0f);
        ApplyReloadCrosshairLineColor(_rightImage, 0f);
        ApplyReloadCrosshairLineColor(_bottomImage, 0f);
        ApplyReloadCrosshairLineColor(_leftImage, 0f);
    }

    private void ApplyReloadCrosshairLineColor(Image image, float pulse)
    {
        if (image == null) return;

        Color baseColor = _reloadCrosshairBaseColor;
        Color pulseColor = _reloadCrosshairPulseColor;
        baseColor.a = image.color.a;
        pulseColor.a = image.color.a;
        image.color = Color.Lerp(baseColor, pulseColor, Mathf.Clamp01(pulse));
    }

    private void RestoreCrosshairLineColors()
    {
        SetImageColor(_topImage, _topBaseColor);
        SetImageColor(_bottomImage, _bottomBaseColor);
        SetImageColor(_leftImage, _leftBaseColor);
        SetImageColor(_rightImage, _rightBaseColor);
    }

    private void ResetReloadCrosshairPulse()
    {
        _topPulseProgress = 0f;
        _rightPulseProgress = 0f;
        _bottomPulseProgress = 0f;
        _leftPulseProgress = 0f;
        ApplySpread();
    }

    private void SetImageColor(Image image, Color color)
    {
        if (image != null) image.color = color;
    }

    private void ApplyReloadCrosshairLineRotation(RectTransform line, float baseAngle)
    {
        if (line == null) return;

        Vector3 eulerAngles = line.localEulerAngles;
        eulerAngles.z = baseAngle + _reloadCrosshairCurrentAngle;
        line.localEulerAngles = eulerAngles;
    }

    private void ApplyReloadCrosshairLineSize(RectTransform line, Vector2 baseSize, float pulse)
    {
        if (line == null) return;

        float squareSize = Mathf.Min(baseSize.x, baseSize.y);
        Vector2 targetSize = new Vector2(squareSize, squareSize);
        Vector2 size = _matchCrosshairLineSizeOnReload
            ? Vector2.Lerp(baseSize, targetSize, _reloadCrosshairSizeProgress)
            : baseSize;
        size *= Mathf.Lerp(1f, _reloadCrosshairPulseScale, Mathf.Clamp01(pulse));
        line.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size.x);
        line.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, size.y);
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
            _reloadCrosshairSizeProgress = Mathf.Approximately(targetAngle, 0f) ? 0f : 1f;
            ApplySpread();
            return;
        }

        _reloadCrosshairRotationCoroutine = StartCoroutine(ReloadCrosshairRotationRoutine(targetAngle, duration));
    }

    private IEnumerator ReloadCrosshairRotationRoutine(float targetAngle, float duration)
    {
        float startAngle = _reloadCrosshairCurrentAngle;
        float startSizeProgress = _reloadCrosshairSizeProgress;
        float targetSizeProgress = Mathf.Approximately(targetAngle, 0f) ? 0f : 1f;

        if (duration <= 0f)
        {
            _reloadCrosshairCurrentAngle = targetAngle;
            _reloadCrosshairSizeProgress = targetSizeProgress;
            _reloadCrosshairRotationCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            _reloadCrosshairCurrentAngle = Mathf.Lerp(startAngle, targetAngle, t);
            _reloadCrosshairSizeProgress = Mathf.Lerp(startSizeProgress, targetSizeProgress, t);
            yield return null;
        }

        _reloadCrosshairCurrentAngle = targetAngle;
        _reloadCrosshairSizeProgress = targetSizeProgress;
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
