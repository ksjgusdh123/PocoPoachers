using UnityEngine;
using UnityEngine.InputSystem;

public class CrosshairUI : MonoBehaviour
{
    [SerializeField] private RectTransform _top, _bottom, _left, _right;

    [Header("스프레드 설정")]
    [SerializeField] private float _pixelsPerDegree = 10f;
    [SerializeField] private float _spreadIncrement = 20f;
    [SerializeField] private float _maxSpread = 150f;
    [SerializeField] private float _recoverySpeed = 80f;

    private RectTransform _rectTransform;
    private float _currentSpread;
    private float _targetBaseSpread;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void Update()
    {
        _rectTransform.position = Mouse.current.position.ReadValue();
        _currentSpread = Mathf.MoveTowards(_currentSpread, _targetBaseSpread, _recoverySpeed * Time.deltaTime);
        ApplySpread();
    }

    public void UpdateBaseSpread(GunData gunData, bool isAiming)
    {
        if (gunData == null) return;
        _targetBaseSpread = (isAiming ? gunData.aimSpreadAngle : gunData.spreadAngle) * _pixelsPerDegree;
    }

    public void OnShoot()
    {
        _currentSpread = Mathf.Min(_currentSpread + _spreadIncrement, _maxSpread);
    }

    private void ApplySpread()
    {
        _top.anchoredPosition    = new Vector2(0,  _currentSpread);
        _bottom.anchoredPosition = new Vector2(0, -_currentSpread);
        _left.anchoredPosition   = new Vector2(-_currentSpread, 0);
        _right.anchoredPosition  = new Vector2( _currentSpread, 0);
    }
}
