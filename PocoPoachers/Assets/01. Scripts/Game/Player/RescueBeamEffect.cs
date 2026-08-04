using System;
using System.Collections;
using UnityEngine;

// 플레이어 완전 사망 시, 위에서 포드가 내려와 빔을 쏘고 플레이어를 위로 호송하는 연출.
// 3D 모델 없이 기본 프리미티브(원반 모양 포드 + 실린더 빔)로 절차적으로 구성한다 (씬에 미리 준비해둘 것 없음).
public class RescueBeamEffect : MonoBehaviour
{
    [Header("Pod")]
    [SerializeField] private float _podHoverHeight = 6f;   // 대상 위 이 높이에서 정지
    [SerializeField] private float _podStartHeight = 20f;  // 이 높이에서부터 내려옴 (화면 밖)
    [SerializeField] private float _podMoveDuration = 1f;
    [SerializeField] private float _podRadius = 1.5f;
    [SerializeField] private Color _podColor = new Color(0.6f, 0.9f, 1f, 1f);

    [Header("Beam")]
    [SerializeField] private float _beamRadius = 0.6f;
    [SerializeField] private float _beamToggleDuration = 0.3f;
    [SerializeField] private float _beamHoldDuration = 1.2f;
    [SerializeField] private Color _beamColor = new Color(0.5f, 0.9f, 1f, 0.5f);

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
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");

        // 포드 — 구를 납작하게 눌러 원반처럼 보이게
        GameObject podGo = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        podGo.name = "RescuePod";
        Destroy(podGo.GetComponent<Collider>());
        podGo.transform.SetParent(transform, false);
        podGo.transform.localScale = new Vector3(_podRadius * 2f, _podRadius * 0.6f, _podRadius * 2f);
        podGo.GetComponent<Renderer>().material = MakeTransparentMaterial(shader, _podColor);
        _pod = podGo.transform;

        // 빔 — 실린더, 포드 바로 아래에서 아래로 자라나도록 피벗을 위쪽에 맞춘다
        GameObject beamRoot = new GameObject("RescueBeamRoot");
        beamRoot.transform.SetParent(_pod, false);

        GameObject beamGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beamGo.name = "RescueBeam";
        Destroy(beamGo.GetComponent<Collider>());
        beamGo.transform.SetParent(beamRoot.transform, false);
        beamGo.transform.localPosition = new Vector3(0f, -0.5f, 0f); // 실린더 기본 높이 2, 피벗이 중앙이라 절반만큼 내려서 위쪽 끝을 포드 위치에 맞춘다
        beamGo.transform.localScale = new Vector3(_beamRadius, 0f, _beamRadius); // 처음엔 높이 0(안 보임)
        beamGo.GetComponent<Renderer>().material = MakeTransparentMaterial(shader, _beamColor);
        _beam = beamGo.transform;
    }

    // URP Unlit 셰이더를 스크립트에서 Transparent 서페이스로 전환 (에디터에서 Surface Type을 Transparent로 바꾸는 것과 동일)
    private Material MakeTransparentMaterial(Shader shader, Color color)
    {
        Material mat = new Material(shader) { color = color };

        mat.SetFloat("_Surface", 1f);
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        return mat;
    }

    private IEnumerator PlaySequence(Transform target, Action onComplete)
    {
        Vector3 basePos = target.position;
        Vector3 startPos = basePos + Vector3.up * _podStartHeight;
        Vector3 hoverPos = basePos + Vector3.up * _podHoverHeight;

        _pod.position = startPos;

        // 1. 포드 하강
        yield return MoveOverTime(p => _pod.position = p, startPos, hoverPos, _podMoveDuration);

        // 2. 빔 켜짐 (포드~바닥까지 닿을 만큼 자라남)
        yield return ScaleBeam(0f, _podHoverHeight, _beamToggleDuration);

        // 3. 대상이 빔 안에서 떠오르다 사라짐
        Renderer[] targetRenderers = target.GetComponentsInChildren<Renderer>();
        Vector3 liftStart = target.position;
        Vector3 liftEnd = liftStart + Vector3.up * _liftHeight;
        yield return MoveOverTime(p => target.position = p, liftStart, liftEnd, _beamHoldDuration);

        foreach (var r in targetRenderers)
            if (r != null) r.enabled = false;

        // 4. 빔 꺼짐 + 포드 이탈
        yield return ScaleBeam(_podHoverHeight, 0f, _beamToggleDuration);

        Vector3 exitPos = hoverPos + Vector3.up * _podStartHeight;
        yield return MoveOverTime(p => _pod.position = p, hoverPos, exitPos, _podMoveDuration);

        onComplete?.Invoke();
        Destroy(gameObject);
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

    private IEnumerator ScaleBeam(float fromLength, float toLength, float duration)
    {
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float length = Mathf.Lerp(fromLength, toLength, t / duration);
            _beam.localScale = new Vector3(_beamRadius, length * 0.5f, _beamRadius);
            yield return null;
        }
        _beam.localScale = new Vector3(_beamRadius, toLength * 0.5f, _beamRadius);
    }
}
