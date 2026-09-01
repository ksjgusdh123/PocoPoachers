using UnityEngine;
using UnityEngine.UI;

public class StaminaWorldUI : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 1.5f, 0f);

    private Transform _playerTransform;
    private PlayerStat _stat;

    public void Setup(PlayerStat stat, Transform playerTransform)
    {
        _stat = stat;
        _playerTransform = playerTransform;

        stat.OnStaminaChanged += Refresh;
        stat.OnDie += Hide;
        stat.OnRevive += ShowIfNeeded;
        Refresh(stat.CurrentStamina, stat.MaxStamina);
    }

    private void OnDestroy()
    {
        if (_stat == null) return;

        _stat.OnStaminaChanged -= Refresh;
        _stat.OnDie -= Hide;
        _stat.OnRevive -= ShowIfNeeded;
    }

    private void LateUpdate()
    {
        if (_playerTransform == null) return;

        transform.position = _playerTransform.position + _offset;
        transform.rotation = CameraSpace.Rotation;
    }

    private void Refresh(float current, float max)
    {
        // 사망 중에는 스태미나가 회복돼도 다시 켜지지 않게 한다
        if (_stat != null && _stat.IsDead)
        {
            gameObject.SetActive(false);
            return;
        }

        float ratio = max > 0f ? current / max : 0f;
        bool isFull = ratio >= 1f;

        gameObject.SetActive(!isFull);

        if (!isFull)
            _fillImage.fillAmount = ratio;
    }

    private void Hide() => gameObject.SetActive(false);

    private void ShowIfNeeded() => Refresh(_stat.CurrentStamina, _stat.MaxStamina);
}
