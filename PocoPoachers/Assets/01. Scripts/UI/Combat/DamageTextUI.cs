using DG.Tweening;
using TMPro;
using UnityEngine;

public class DamageTextUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;

    private Sequence _sequence;

    public void Play(float damage, Vector3 worldPosition)
    {
        _text.text = Mathf.RoundToInt(damage).ToString();
        transform.position = worldPosition;
        transform.localScale = Vector3.zero;

        Color color = _text.color;
        color.a = 1f;
        _text.color = color;

        Vector3 towardCamera = -Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;
        float randomSide = Random.Range(-1f, 1f);

        Vector3 popTarget = worldPosition + Vector3.up * 1f + towardCamera * 0.5f;
        Vector3 fallTarget = popTarget + Vector3.down * 1f + cameraRight * randomSide;

        _sequence?.Kill();
        _sequence = DOTween.Sequence()
            // 페이즈 1: 커지면서 위로 + 카메라 방향으로 튀어나옴
            .Append(transform.DOScale(2f, 0.2f).SetEase(Ease.OutBack))
            .Join(transform.DOMove(popTarget, 0.2f).SetEase(Ease.OutCubic))
            // 페이즈 2: 작아지면서 추락 + 페이드아웃
            .Append(transform.DOScale(0.3f, 0.4f).SetEase(Ease.InCubic))
            .Join(transform.DOMove(fallTarget, 0.4f).SetEase(Ease.InCubic))
            .Join(_text.DOFade(0f, 0.4f))
            .OnComplete(() => DamageTextPool.Instance.Return(this));
    }
}
