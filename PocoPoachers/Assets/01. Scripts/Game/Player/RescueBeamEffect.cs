using System;
using System.Collections;
using UnityEngine;

// 플레이어 완전 사망 시, 뒤에서 포드(호송선)가 날아와 빔을 쏘고 플레이어를 위로 호송하는 연출.
// 포드/빔 모델은 RescueEffectPrefabs에 인스펙터로 지정해둔 프리팹을 사용한다.
public class RescueBeamEffect : MonoBehaviour
{
    [Header("Pod")]
    [SerializeField] private float _podHoverHeight = 6f;   // 대상 위 이 높이에서 정지
    // 접근/이탈 거리와 시간은 원래 속도(약 67m/s)를 유지하도록 같은 비율로 맞춰져 있다.
    // 한쪽만 바꾸면 포드가 느려지거나 빨라진다.
    [SerializeField] private float _podApproachDistance = 200f; // 대상 뒤 이 거리(수평)에서부터 날아옴 (화면 밖)
    [SerializeField] private float _podApproachDuration = 3f;   // 로켓 사운드 길이에 맞춰 잡은 값
    [SerializeField] private float _podDepartDuration = 3f;
    [SerializeField] private float _podRotateDuration = 1f; // 이탈 전 온 방향으로 되도는 데 걸리는 시간
    [SerializeField] private Vector3 _podModelScale = Vector3.one;
    [SerializeField] private Vector3 _podModelEulerAngles = Vector3.zero;

    [Header("Beam")]
    [SerializeField] private float _beamToggleDuration = 0.3f;
    [SerializeField] private float _beamHoldDuration = 1.2f;

    [Header("Lift")]
    [SerializeField] private float _liftHeight = 5f; // 플레이어가 빔 안에서 떠오르는 높이

    [Header("Trail")]
    [SerializeField] private float _trailTime = 0.4f;
    [SerializeField] private float _trailStartWidth = 0.6f;
    [SerializeField] private float _trailEndWidth = 0.05f;
    [SerializeField] private Color _trailColor = new Color(0.5f, 0.9f, 1f, 0.8f);

    [Header("Sound")]
    [SerializeField, Tooltip("sound.csv의 키. 비워두거나 테이블에 없으면 소리를 내지 않는다.")]
    private string _podSoundKey = "sfx_rescue_pod";
    [SerializeField] private float _podSoundMaxDistance = 220f; // 접근 거리보다 넉넉히 — 출발 지점부터 희미하게 들리도록

    [Header("Ghost")]
    [SerializeField] private float _ghostInterval = 0.08f;    // 잔상 생성 간격
    [SerializeField] private float _ghostFadeDuration = 0.3f; // 잔상 하나가 옅어져 사라지는 데 걸리는 시간
    [SerializeField] private Color _ghostColor = new Color(0.5f, 0.9f, 1f, 0.4f);

    private Transform _pod;
    private Transform _beam;
    private TrailRenderer _trail;
    private Coroutine _ghostRoutine;

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

        // 트레일 — 이동 중일 때만 emitting을 켜서 정지/텔레포트 중엔 잔상이 안 남게 한다
        _trail = podGo.AddComponent<TrailRenderer>();
        _trail.time = _trailTime;
        _trail.startWidth = _trailStartWidth;
        _trail.endWidth = _trailEndWidth;
        _trail.material = new Material(Shader.Find("Sprites/Default"));
        _trail.startColor = _trailColor;
        _trail.endColor = new Color(_trailColor.r, _trailColor.g, _trailColor.b, 0f);
        _trail.emitting = false;

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

        // 1. 스폰 순간부터 대상 쪽을 보게 두고, 곧장 뒤에서 날아와 대상 위로 이동
        Quaternion approachRotation = Quaternion.LookRotation(-behindDir, Vector3.up) * Quaternion.Euler(_podModelEulerAngles);
        _pod.SetPositionAndRotation(startPos, approachRotation);
        _trail.Clear(); // 배치 직후 바로 켜므로, 생성 위치(월드 원점)에서 끌려온 궤적이 남지 않게 비운다

        PlayPodSfx();
        _trail.emitting = true;
        _ghostRoutine = StartCoroutine(GhostSpawnLoop());
        yield return MoveOverTime(p => _pod.position = p, startPos, hoverPos, _podApproachDuration);
        StopCoroutine(_ghostRoutine);
        _trail.emitting = false;

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
        PlayPodSfx();
        _trail.emitting = true;
        _ghostRoutine = StartCoroutine(GhostSpawnLoop());
        yield return MoveOverTime(p => _pod.position = p, hoverPos, startPos, _podDepartDuration);

        onComplete?.Invoke();
        Destroy(gameObject);
    }

    // 소리는 포드에 붙여서 낸다 — 멀리서 날아와 멀어지는 게 거리감으로 들리도록.
    // 포드가 파괴되면 소리도 끊기지만, 그 시점엔 이미 멀어져 거의 안 들린다.
    private void PlayPodSfx()
    {
        if (string.IsNullOrEmpty(_podSoundKey)) return;

        SoundManager.GetInstance()?.PlaySfxOn(_podSoundKey, _pod, _podSoundMaxDistance);
    }

    // 이동 중 일정 간격으로 포드 모델의 반투명 잔상을 남긴다
    private IEnumerator GhostSpawnLoop()
    {
        while (true)
        {
            SpawnGhost();
            yield return new WaitForSeconds(_ghostInterval);
        }
    }

    private void SpawnGhost()
    {
        GameObject podPrefab = RescueEffectPrefabs.Instance != null ? RescueEffectPrefabs.Instance.PodPrefab : null;
        if (podPrefab == null) return;

        GameObject ghost = Instantiate(podPrefab, _pod.position, _pod.rotation);
        ghost.name = "RescuePodGhost";
        ghost.transform.localScale = _pod.lossyScale;

        foreach (var c in ghost.GetComponentsInChildren<Collider>())
            Destroy(c);

        Material ghostMaterial = MakeGhostMaterial();
        foreach (var r in ghost.GetComponentsInChildren<Renderer>())
            r.material = ghostMaterial;

        // 페이드/파괴는 잔상 자신에 붙여서 진행 — 포드(this)가 먼저 파괴돼도 끊기지 않게 한다
        ghost.AddComponent<RescuePodGhostFade>().Init(ghostMaterial, _ghostFadeDuration);
    }

    private Material MakeGhostMaterial()
    {
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Unlit")) { color = _ghostColor };

        mat.SetFloat("_Surface", 1f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return mat;
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

    private IEnumerator MoveOverTime(Action<Vector3> apply, Vector3 from, Vector3 to, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            apply(Vector3.Lerp(from, to, t / duration));
            yield return null;
        }
        apply(to);
    }

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
