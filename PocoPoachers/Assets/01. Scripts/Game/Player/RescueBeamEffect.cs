using System;
using System.Collections;
using UnityEngine;

// 플레이어 완전 사망 시, 뒤에서 포드(호송선)가 날아와 빔을 쏘고 플레이어를 위로 호송하는 연출.
// 포드/빔 모델은 RescueEffectPrefabs에 인스펙터로 지정해둔 프리팹을 사용한다.
public class RescueBeamEffect : MonoBehaviour
{
    [Header("Pod")]
    [SerializeField] private float _podHoverHeight = 6f;   // 대상 위 이 높이에서 정지
    [SerializeField] private float _podApproachDistance = 20f; // 대상 뒤 이 거리(수평)에서부터 날아옴 (화면 밖)
    [SerializeField] private float _podMoveDuration = 1f;
    [SerializeField] private float _podRotateDuration = 1f; // 이동 시작 전 진행 방향으로 회전하는 데 걸리는 시간
    [SerializeField] private Vector3 _podModelScale = Vector3.one;
    [SerializeField] private Vector3 _podModelEulerAngles = Vector3.zero;

    [Header("Beam")]
    [SerializeField] private float _beamToggleDuration = 0.3f;
    [SerializeField] private float _beamHoldDuration = 1.2f;

    [Header("Lift")]
    [SerializeField] private float _liftHeight = 5f; // 플레이어가 빔 안에서 떠오르는 높이

    private Transform _pod;
    private Transform _beam;

    public void Play(Transform target, Action onComplete)
    {
        BuildVisuals();
        StartCoroutine(PlaySequence(target, onComplete));
    }

    private void BuildVisuals()
    {
        var prefabs = RescueEffectPrefabs.Instance;

        // 포드 — 인스펙터로 지정된 프리팹을 그대로 사용한다
        GameObject podPrefab = prefabs != null ? prefabs.PodPrefab : null;
        GameObject podGo;
        if (podPrefab != null)
        {
            podGo = Instantiate(podPrefab, transform);
            podGo.transform.localPosition = Vector3.zero;
            podGo.transform.localRotation = Quaternion.Euler(_podModelEulerAngles);
            podGo.transform.localScale = _podModelScale;
        }
        else
        {
            podGo = new GameObject("RescuePod");
            podGo.transform.SetParent(transform, false);
            Debug.LogWarning("[RescueBeamEffect] RescueEffectPrefabs에 포드 프리팹이 지정되지 않았습니다.");
        }
        _pod = podGo.transform;

        // 빔 — 인스펙터로 지정된 프리팹을 그대로 사용한다.
        // 피벗(로컬 y=0)이 포드 위치, 아래(-Y)로 1유닛 길이로 모델링되어 있어야 한다 — localScale.y로 길이를 조절한다.
        GameObject beamPrefab = prefabs != null ? prefabs.BeamPrefab : null;
        GameObject beamGo;
        if (beamPrefab != null)
        {
            beamGo = Instantiate(beamPrefab, _pod);
            beamGo.transform.localPosition = Vector3.zero;
            beamGo.transform.localRotation = Quaternion.identity;
            beamGo.transform.localScale = new Vector3(1f, 0f, 1f); // 처음엔 길이 0(안 보임)
        }
        else
        {
            beamGo = new GameObject("RescueBeam");
            beamGo.transform.SetParent(_pod, false);
            Debug.LogWarning("[RescueBeamEffect] RescueEffectPrefabs에 빔 프리팹이 지정되지 않았습니다.");
        }
        _beam = beamGo.transform;
    }

    private IEnumerator PlaySequence(Transform target, Action onComplete)
    {
        Vector3 basePos = target.position;

        // 대상이 바라보는 방향의 반대(뒤쪽)을 수평으로 계산 — 대상이 위/아래를 봐도 포드는 수평으로만 접근한다
        Vector3 behindDir = new Vector3(-target.forward.x, 0f, -target.forward.z);
        if (behindDir.sqrMagnitude < 0.0001f) behindDir = Vector3.back;
        behindDir.Normalize();

        Vector3 hoverPos = basePos + Vector3.up * _podHoverHeight;
        Vector3 startPos = hoverPos + behindDir * _podApproachDistance;

        _pod.position = startPos;

        // 1. 진행 방향으로 회전을 마친 뒤, 포드가 뒤에서 날아와 대상 위로 이동
        Quaternion approachRotation = Quaternion.LookRotation(-behindDir, Vector3.up) * Quaternion.Euler(_podModelEulerAngles);
        yield return RotateOverTime(_pod.rotation, approachRotation, _podRotateDuration);
        yield return MoveOverTime(p => _pod.position = p, startPos, hoverPos, _podMoveDuration);

        // 2. 빔 켜짐 (포드~바닥까지 닿을 만큼 자라남)
        yield return ScaleBeam(0f, _podHoverHeight, _beamToggleDuration);

        // 3. 대상이 빔 안에서 떠오르며 점점 작아지다 사라짐
        Renderer[] targetRenderers = target.GetComponentsInChildren<Renderer>();
        Vector3 liftStart = target.position;
        Vector3 liftEnd = liftStart + Vector3.up * _liftHeight;
        Vector3 liftStartScale = target.localScale;
        yield return LiftAndShrink(target, liftStart, liftEnd, liftStartScale, _beamHoldDuration);

        foreach (var r in targetRenderers)
            if (r != null) r.enabled = false;

        // 4. 빔 꺼짐 + 온 방향(뒤)으로 회전을 마친 뒤 이탈
        yield return ScaleBeam(_podHoverHeight, 0f, _beamToggleDuration);

        Quaternion departRotation = Quaternion.LookRotation(behindDir, Vector3.up) * Quaternion.Euler(_podModelEulerAngles);
        yield return RotateOverTime(_pod.rotation, departRotation, _podRotateDuration);
        yield return MoveOverTime(p => _pod.position = p, hoverPos, startPos, _podMoveDuration, EaseInQuad);

        onComplete?.Invoke();
        Destroy(gameObject);
    }

    private IEnumerator LiftAndShrink(Transform target, Vector3 from, Vector3 to, Vector3 startScale, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            target.position = Vector3.Lerp(from, to, p);
            target.localScale = Vector3.Lerp(startScale, Vector3.zero, p);
            yield return null;
        }
        target.position = to;
        target.localScale = Vector3.zero;
    }

    private IEnumerator MoveOverTime(Action<Vector3> apply, Vector3 from, Vector3 to, float duration, Func<float, float> ease = null)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = t / duration;
            apply(Vector3.Lerp(from, to, ease != null ? ease(p) : p));
            yield return null;
        }
        apply(to);
    }

    // 이륙하듯 천천히 출발해서 점점 빨라지는 가속 곡선
    private static float EaseInQuad(float t) => t * t;

    private IEnumerator RotateOverTime(Quaternion from, Quaternion to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            _pod.rotation = Quaternion.Slerp(from, to, t / duration);
            yield return null;
        }
        _pod.rotation = to;
    }

    private IEnumerator ScaleBeam(float fromLength, float toLength, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float length = Mathf.Lerp(fromLength, toLength, t / duration);
            _beam.localScale = new Vector3(1f, length, 1f);
            yield return null;
        }
        _beam.localScale = new Vector3(1f, toLength, 1f);
    }
}
