using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// 배경에서 운석 이미지가 화면 밖에서 나타나 대각선으로 슥 지나가고 사라지는 걸 반복 스폰한다.
// 오브젝트를 실제로 움직이는 대신, 배경 요소가 스쳐 지나가는 걸로 전진하는 느낌을 준다.
public class PassingMeteorSpawner : MonoBehaviour
{
    [SerializeField] private RectTransform _spawnArea; // 운석이 지나다닐 영역 (보통 로딩 캔버스 전체)
    [SerializeField] private Image _meteorPrefab;

    [Header("Direction")]
    [SerializeField] private Vector2 _direction = new Vector2(-1f, -0.4f); // 오른쪽 위 -> 왼쪽 아래로 스침
    [SerializeField] private float _edgeMargin = 150f; // 화면 밖에서 시작/종료되도록 여유를 주는 거리

    [Header("Timing")]
    [SerializeField] private float _minInterval = 0.4f;
    [SerializeField] private float _maxInterval = 1.5f;
    [SerializeField] private float _minDuration = 0.5f;
    [SerializeField] private float _maxDuration = 1.1f;

    [Header("Depth (가운데 큰 유성 vs 외곽 작은 유성)")]
    [SerializeField] private float _centerBandWidth = 200f; // 중심선에서 이 폭 안은 항상 큰 유성만 나온다
    [SerializeField] private float _centerScaleMin = 1.0f;
    [SerializeField] private float _centerScaleMax = 1.4f;
    [SerializeField] private float _edgeScaleMin = 0.3f;
    [SerializeField] private float _edgeScaleMax = 0.6f;

    [Header("Look")]
    [SerializeField] private bool _alignRotationToDirection = true;
    [SerializeField] private float _fadeInOutRatio = 0.15f; // 진행률 앞/뒤 이 비율 구간에서 알파 페이드

    private Vector2 _originalDirection;
    private Coroutine _loopCoroutine;

    private void Awake()
    {
        _originalDirection = _direction;
    }

    // 로딩 방향(예: Shelter로 돌아올 때 vs 나갈 때)에 따라 운석이 스치는 방향을 반대로 뒤집는다
    public void SetReversed(bool reversed) => _direction = reversed ? -_originalDirection : _originalDirection;

    private void OnEnable() => _loopCoroutine = StartCoroutine(SpawnLoop());

    private void OnDisable()
    {
        if (_loopCoroutine != null) StopCoroutine(_loopCoroutine);
        _loopCoroutine = null;
    }

    private IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(_minInterval, _maxInterval));
            SpawnOne();
        }
    }

    private void SpawnOne()
    {
        if (_spawnArea == null || _meteorPrefab == null) return;

        Vector2 dir = _direction.normalized;
        Vector2 perpendicular = new Vector2(-dir.y, dir.x);

        Rect rect = _spawnArea.rect;

        // SpawnArea(+margin) 사각형 경계를 dir 방향으로 나아갈 때 정확히 벗어나는 지점까지의 거리
        float extentX = rect.width * 0.5f + _edgeMargin;
        float extentY = rect.height * 0.5f + _edgeMargin;
        float tx = Mathf.Abs(dir.x) > 0.0001f ? extentX / Mathf.Abs(dir.x) : float.MaxValue;
        float ty = Mathf.Abs(dir.y) > 0.0001f ? extentY / Mathf.Abs(dir.y) : float.MaxValue;
        float halfExtent = Mathf.Min(tx, ty);

        // perpendicular 방향 오프셋도 SpawnArea 폭을 벗어나지 않는 범위로 제한
        float laneRange = rect.width * 0.5f * Mathf.Abs(perpendicular.x) + rect.height * 0.5f * Mathf.Abs(perpendicular.y);
        float lane = Random.Range(-laneRange, laneRange);

        Vector2 center = rect.center;
        Vector2 startPos = center - dir * halfExtent + perpendicular * lane;
        Vector2 endPos = center + dir * halfExtent + perpendicular * lane;

        Image meteor = Instantiate(_meteorPrefab, _spawnArea);
        RectTransform rt = meteor.rectTransform;
        rt.anchoredPosition = startPos;

        rt.localScale = Vector3.one * GetScaleForLane(lane, laneRange);

        if (_alignRotationToDirection)
        {
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        Color baseColor = meteor.color;
        meteor.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

        float duration = Random.Range(_minDuration, _maxDuration);
        Sequence seq = DOTween.Sequence().SetUpdate(true);
        seq.Append(rt.DOAnchorPos(endPos, duration).SetEase(Ease.Linear));
        seq.Join(meteor.DOFade(1f, duration * _fadeInOutRatio).SetEase(Ease.Linear));
        seq.Insert(duration * (1f - _fadeInOutRatio), meteor.DOFade(0f, duration * _fadeInOutRatio).SetEase(Ease.Linear));
        seq.OnComplete(() =>
        {
            if (meteor != null) Destroy(meteor.gameObject);
        });
    }

    // 중심선(lane=0)에서 가까울수록 크게, _centerBandWidth 밖으로 나가면 laneRange(외곽)까지 점점 작아진다
    private float GetScaleForLane(float lane, float laneRange)
    {
        float halfCenterBand = _centerBandWidth * 0.5f;
        float absLane = Mathf.Abs(lane);

        if (absLane <= halfCenterBand)
            return Random.Range(_centerScaleMin, _centerScaleMax);

        float t = laneRange > halfCenterBand
            ? Mathf.InverseLerp(halfCenterBand, laneRange, absLane)
            : 1f;

        float centerScale = Random.Range(_centerScaleMin, _centerScaleMax);
        float edgeScale = Random.Range(_edgeScaleMin, _edgeScaleMax);
        return Mathf.Lerp(centerScale, edgeScale, t);
    }
}
