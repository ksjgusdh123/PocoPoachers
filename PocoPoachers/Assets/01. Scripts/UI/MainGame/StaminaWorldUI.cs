using UnityEngine;
using UnityEngine.UI;

public class StaminaWorldUI : MonoBehaviour
{
    [SerializeField] private Image _fillImage;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 1.5f, 0f);

    private Transform _playerTransform;

    public void Setup(PlayerStat stat, Transform playerTransform)
    {
        _playerTransform = playerTransform;

        stat.OnStaminaChanged += Refresh;
        Refresh(stat.CurrentStamina, stat.MaxStamina);
    }

    private void LateUpdate()
    {
        if (_playerTransform != null)
            transform.position = _playerTransform.position + _offset;
    }

    private void Refresh(float current, float max)
    {
        float ratio = max > 0f ? current / max : 0f;
        bool isFull = ratio >= 1f;

        gameObject.SetActive(!isFull);

        if (!isFull)
            _fillImage.fillAmount = ratio;
    }
}
