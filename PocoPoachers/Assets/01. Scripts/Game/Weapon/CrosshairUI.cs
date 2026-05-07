using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
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
    [SerializeField] private float _kickRecovery = 150f;

    private Vector2 _recoilOffset;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void Update()
    {
        _recoilOffset = Vector2.MoveTowards(_recoilOffset, Vector2.zero, _kickRecovery * Time.deltaTime);

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 crosshairPos = new Vector2(
            Mathf.Clamp(mousePos.x + _recoilOffset.x, 0f, Screen.width),
            Mathf.Clamp(mousePos.y + _recoilOffset.y, 0f, Screen.height));
        _recoilOffset = crosshairPos - mousePos;

        if (_recoilOffset.sqrMagnitude > 0.01f && Mouse.current.delta.ReadValue().sqrMagnitude > 0f)
        {
            Mouse.current.WarpCursorPosition(crosshairPos);
            _recoilOffset = Vector2.zero;
        }

        _rectTransform.position = crosshairPos;

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

    }

    public void ResetSpread()
    {
        _isCollapsing = true;
    }

    public void OnShoot(Vector2 kickVector)
    {
        _currentSpread = Mathf.Min(_currentSpread + _spreadIncrement, _maxSpread);
        _recoilOffset += kickVector;
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
        //Cursor.lockState = isGameMode ? CursorLockMode.Confined : CursorLockMode.None;
    }
}
