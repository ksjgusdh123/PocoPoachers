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
    [SerializeField] private float _hitMarkerDistance = 12f;
    [SerializeField] private float _hitMarkerStartDistance = 20f;
    [SerializeField] private float _hitMarkerEndDistance = 28f;
    [SerializeField] private float _hitMarkerConvergeTime = 0.04f;
    [SerializeField] private Vector2 _hitMarkerLineSize = new Vector2(14f, 2f);

    private Vector2 _recoilTarget;
    private Vector2 _recoilOffset;
    private float _shakeIntensity;
    private float _shakeDuration;
    private float _shakeTimer;
    private float _shakeAngle;
    private float _hitMarkerTimer;
    private RectTransform[] _hitMarkerLines;

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

        Vector2 targetPos = new Vector2(
            Mathf.Clamp(mousePos.x + _recoilTarget.x, 0f, Screen.width),
            Mathf.Clamp(mousePos.y + _recoilTarget.y, 0f, Screen.height));
        _recoilTarget = targetPos - mousePos;

        Vector2 crosshairPos = new Vector2(
            Mathf.Clamp(mousePos.x + _recoilOffset.x, 0f, Screen.width),
            Mathf.Clamp(mousePos.y + _recoilOffset.y, 0f, Screen.height));
        _recoilOffset = crosshairPos - mousePos;

        if (_recoilOffset.sqrMagnitude > 0.01f && Mouse.current.delta.ReadValue().sqrMagnitude > 0f)
        {
            Mouse.current.WarpCursorPosition(crosshairPos);
            _recoilOffset = Vector2.zero;
            _recoilTarget = Vector2.zero;
        }

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
        ApplyHitMarkerDistance(_hitMarkerStartDistance);
    }

    private void UpdateHitMarker()
    {
        if (_hitMarkerGroup == null) return;

        if (_hitMarkerTimer > 0f)
        {
            _hitMarkerTimer -= Time.deltaTime;
            _hitMarkerGroup.alpha = 1f;
            UpdateHitMarkerDistance();
            return;
        }

        ApplyHitMarkerDistance(_hitMarkerEndDistance);
        _hitMarkerGroup.alpha = Mathf.MoveTowards(_hitMarkerGroup.alpha, 0f, _hitMarkerFadeSpeed * Time.deltaTime);
    }

    private void UpdateHitMarkerDistance()
    {
        if (_hitMarkerDuration <= 0f)
        {
            ApplyHitMarkerDistance(_hitMarkerDistance);
            return;
        }

        float elapsed = _hitMarkerDuration - _hitMarkerTimer;
        float distance;

        if (elapsed < _hitMarkerConvergeTime)
        {
            float t = _hitMarkerConvergeTime > 0f ? elapsed / _hitMarkerConvergeTime : 1f;
            distance = Mathf.Lerp(_hitMarkerStartDistance, _hitMarkerDistance, t);
        }
        else
        {
            float expandDuration = Mathf.Max(_hitMarkerDuration - _hitMarkerConvergeTime, 0.001f);
            float t = (elapsed - _hitMarkerConvergeTime) / expandDuration;
            distance = Mathf.Lerp(_hitMarkerDistance, _hitMarkerEndDistance, t);
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
        if (_hitMarkerGroup != null) return;

        Transform existing = transform.Find("HitMarker");
        if (existing != null && existing.TryGetComponent(out _hitMarkerGroup))
        {
            CacheHitMarkerLines(existing);
            _hitMarkerGroup.alpha = 0f;
            return;
        }

        GameObject groupObject = new GameObject("HitMarker", typeof(RectTransform), typeof(CanvasGroup));
        RectTransform groupRect = groupObject.GetComponent<RectTransform>();
        groupRect.SetParent(_rectTransform, false);
        groupRect.anchorMin = new Vector2(0.5f, 0.5f);
        groupRect.anchorMax = new Vector2(0.5f, 0.5f);
        groupRect.anchoredPosition = Vector2.zero;
        groupRect.sizeDelta = Vector2.zero;

        _hitMarkerGroup = groupObject.GetComponent<CanvasGroup>();
        _hitMarkerGroup.alpha = 0f;
        _hitMarkerGroup.blocksRaycasts = false;
        _hitMarkerGroup.interactable = false;

        _hitMarkerLines = new RectTransform[4];
        _hitMarkerLines[0] = CreateHitMarkerLine(groupRect, "TopLeft", new Vector2(-_hitMarkerDistance, _hitMarkerDistance), -45f);
        _hitMarkerLines[1] = CreateHitMarkerLine(groupRect, "TopRight", new Vector2(_hitMarkerDistance, _hitMarkerDistance), 45f);
        _hitMarkerLines[2] = CreateHitMarkerLine(groupRect, "BottomLeft", new Vector2(-_hitMarkerDistance, -_hitMarkerDistance), 45f);
        _hitMarkerLines[3] = CreateHitMarkerLine(groupRect, "BottomRight", new Vector2(_hitMarkerDistance, -_hitMarkerDistance), -45f);
    }

    private void CacheHitMarkerLines(Transform hitMarker)
    {
        _hitMarkerLines = new RectTransform[4];
        _hitMarkerLines[0] = hitMarker.Find("TopLeft") as RectTransform;
        _hitMarkerLines[1] = hitMarker.Find("TopRight") as RectTransform;
        _hitMarkerLines[2] = hitMarker.Find("BottomLeft") as RectTransform;
        _hitMarkerLines[3] = hitMarker.Find("BottomRight") as RectTransform;
    }

    private RectTransform CreateHitMarkerLine(RectTransform parent, string name, Vector2 anchoredPosition, float rotation)
    {
        GameObject lineObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        RectTransform lineRect = lineObject.GetComponent<RectTransform>();
        lineRect.SetParent(parent, false);
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = anchoredPosition;
        lineRect.sizeDelta = _hitMarkerLineSize;
        lineRect.localEulerAngles = new Vector3(0f, 0f, rotation);

        Image lineImage = lineObject.GetComponent<Image>();
        lineImage.color = _hitMarkerColor;
        lineImage.raycastTarget = false;

        return lineRect;
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
