using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 오브젝트가 제자리에서 위아래로 까닥이고 좌우로 살짝 기우뚱거리며 떠 있는 듯한 느낌을 준다.
// 로딩 화면의 로켓처럼, 실제로 이동시키지 않고도 살아있는 느낌/전진하는 느낌을 보조하는 용도.
// RectTransform(UI)이든 일반 Transform(SpriteRenderer)이든 붙은 오브젝트 그대로 동작한다.
public class IdleFloatMotion : MonoBehaviour
{
    [SerializeField] private float _bobDistance = 15f;
    [SerializeField] private float _bobDuration = 1.2f;
    [SerializeField] private float _tiltAngle = 4f;
    [SerializeField] private float _tiltDuration = 1.6f;

    private RectTransform _rect;
    private Vector2 _baseAnchoredPos;
    private Vector3 _baseLocalPos;
    private Vector3 _baseLocalEuler; // 로켓처럼 이미 기본 기울기가 잡혀 있는 오브젝트도 그 각도를 기준으로 흔들리게 함
    private Sequence _sequence;

    private void Awake()
    {
        _rect = transform as RectTransform;
        if (_rect != null) _baseAnchoredPos = _rect.anchoredPosition;
        else _baseLocalPos = transform.localPosition;
        _baseLocalEuler = transform.localEulerAngles;
    }

    private void OnEnable() => Play();

    private void OnDisable()
    {
        _sequence?.Kill();
        _sequence = null;

        transform.localEulerAngles = _baseLocalEuler;
        if (_rect != null) _rect.anchoredPosition = _baseAnchoredPos;
        else transform.localPosition = _baseLocalPos;
    }

    private void Play()
    {
        _sequence?.Kill();
        _sequence = DOTween.Sequence().SetUpdate(true);

        // 1구간: 위로 까닥 + 오른쪽으로 기울임 (동시 재생)
        if (_rect != null)
            _sequence.Append(_rect.DOAnchorPosY(_baseAnchoredPos.y + _bobDistance, _bobDuration).SetEase(Ease.InOutSine));
        else
            _sequence.Append(transform.DOLocalMoveY(_baseLocalPos.y + _bobDistance, _bobDuration).SetEase(Ease.InOutSine));
        _sequence.Join(transform.DOLocalRotate(_baseLocalEuler + new Vector3(0, 0, _tiltAngle), _tiltDuration).SetEase(Ease.InOutSine));

        // 2구간: 아래로 까닥 + 원래 각도로 복귀 (동시 재생)
        if (_rect != null)
            _sequence.Append(_rect.DOAnchorPosY(_baseAnchoredPos.y, _bobDuration).SetEase(Ease.InOutSine));
        else
            _sequence.Append(transform.DOLocalMoveY(_baseLocalPos.y, _bobDuration).SetEase(Ease.InOutSine));
        _sequence.Join(transform.DOLocalRotate(_baseLocalEuler, _tiltDuration).SetEase(Ease.InOutSine));

        _sequence.SetLoops(-1, LoopType.Restart);
    }

    // 로딩이 끝나 다음 씬으로 넘어가기 직전, 로켓이 바라보는 방향(transform.up)으로 가속하며
    // 튀어나가듯 커지고 페이드아웃되는 발사 연출. 끝나는 시점을 알아야 하는 호출부를 위해 Tween을 반환한다.
    public Tween PlayLaunch(float duration = 0.5f, float distance = 800f, float scaleMultiplier = 1.6f)
    {
        _sequence?.Kill();

        Vector3 forward = transform.up;
        Sequence launch = DOTween.Sequence().SetUpdate(true);

        if (_rect != null)
            launch.Join(_rect.DOAnchorPos(_rect.anchoredPosition + (Vector2)(forward * distance), duration).SetEase(Ease.InQuad));
        else
            launch.Join(transform.DOLocalMove(transform.localPosition + forward * distance, duration).SetEase(Ease.InQuad));

        launch.Join(transform.DOScale(transform.localScale * scaleMultiplier, duration).SetEase(Ease.InQuad));

        Graphic graphic = GetComponent<Graphic>();
        if (graphic != null)
        {
            launch.Join(graphic.DOFade(0f, duration).SetEase(Ease.InQuad));
        }
        else
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null) launch.Join(sr.DOFade(0f, duration).SetEase(Ease.InQuad));
        }

        // 완전히 페이드아웃된 시점에 오브젝트를 꺼서, 자식으로 붙은 화염 등도 함께 안 그려지게 한다.
        // OnComplete 대신 AppendCallback을 쓰는 이유: 호출부(LoadingSceneController)가 이 Tween에
        // 자기 OnComplete를 따로 거는데, OnComplete는 마지막 호출로 덮어써지므로 여기서 겹치면 안 된다.
        launch.AppendCallback(() => gameObject.SetActive(false));

        return launch;
    }
}
