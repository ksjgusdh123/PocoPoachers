using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 은신 시 몸을 반투명하게 만드는 순수 시각 효과. 게임플레이(탐지 회피)는 StatBase.IsStealthed/
// TargetDetector가 담당하고, 이건 그 상태를 화면에 보여주기만 한다(모든 클라이언트에서 재생).
// 필요할 때 대상 GameObject에 동적으로 붙는다(RemotePlayerStat과 같은 방식) — 플레이어 프리팹에
// 미리 붙어 있지 않아도 된다.
//
// 렌더러 전체(GetComponentsInChildren<Renderer>)를 대상으로 하므로, 플레이어 자식에 몸통 외
// 렌더러(네임플레이트 등)가 있다면 같이 반투명해질 수 있다 — 문제가 되면 대상 범위를 좁혀야 한다.
public class StealthVisual : MonoBehaviour
{
    private const float FadeDuration = 0.35f;

    private readonly Dictionary<Renderer, Material[]> _originalMaterials = new();
    private readonly List<Material> _fadeMaterials = new(); // 색 프로퍼티가 있어 실제로 알파를 조절할 수 있는 클론만
    private bool _active;
    private float _currentAlpha = 1f;
    private float _targetAlpha = 1f;
    private Coroutine _fadeRoutine;

    public static void SetActiveFor(int playerId, bool active, float alpha)
    {
        var om = ObjectManager.Instance;
        if (om == null || !om.TryGet(ObjectKind.Player, playerId, out var playerObj)) return;

        Get(playerObj.gameObject).Apply(active, alpha);
    }

    public static void SetActiveForSelf(GameObject self, bool active, float alpha)
    {
        if (self == null) return;
        Get(self).Apply(active, alpha);
    }

    private static StealthVisual Get(GameObject target)
    {
        return target.TryGetComponent<StealthVisual>(out var visual) ? visual : target.AddComponent<StealthVisual>();
    }

    private void Apply(bool active, float alpha)
    {
        if (_active == active) return;
        _active = active;

        if (active)
        {
            _targetAlpha = Mathf.Clamp01(alpha);
            // 페이드아웃 도중 다시 켜졌으면(클론이 아직 남아있음) 새로 만들지 않고 이어서 페이드한다
            if (_originalMaterials.Count == 0)
                MakeTransparentMaterials();
            RestartFade(_currentAlpha, _targetAlpha, restoreAfter: false);
        }
        else
        {
            RestartFade(_currentAlpha, 1f, restoreAfter: true);
        }
    }

    private void RestartFade(float from, float to, bool restoreAfter)
    {
        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeRoutine(from, to, restoreAfter));
    }

    private IEnumerator FadeRoutine(float from, float to, bool restoreAfter)
    {
        float t = 0f;
        while (t < FadeDuration)
        {
            t += Time.deltaTime;
            SetAlpha(Mathf.Lerp(from, to, t / FadeDuration));
            yield return null;
        }
        SetAlpha(to);

        if (restoreAfter) Restore();
        _fadeRoutine = null;
    }

    private void SetAlpha(float alpha)
    {
        _currentAlpha = alpha;
        foreach (var mat in _fadeMaterials)
        {
            if (mat == null) continue;
            Color c = mat.color;
            mat.color = new Color(c.r, c.g, c.b, alpha);
        }
    }

    private void MakeTransparentMaterials()
    {
        _originalMaterials.Clear();
        _fadeMaterials.Clear();

        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            Material[] shared = renderer.sharedMaterials;
            _originalMaterials[renderer] = shared;

            var clones = new Material[shared.Length];
            for (int i = 0; i < clones.Length; i++)
            {
                Material clone = MakeTransparentClone(shared[i]);
                clones[i] = clone;
                if (clone != shared[i]) _fadeMaterials.Add(clone); // 색 프로퍼티가 있어 실제로 바뀐 것만 애니메이션 대상
            }
            renderer.sharedMaterials = clones;
        }
    }

    private void Restore()
    {
        foreach (var kv in _originalMaterials)
            if (kv.Key != null) kv.Key.sharedMaterials = kv.Value;
        _originalMaterials.Clear();
        _fadeMaterials.Clear();
    }

    // RescueBeamEffect의 잔상 머티리얼과 같은 URP 투명 전환 레시피 — 원본 셰이더/텍스처·색은 그대로 두고
    // 블렌드 모드만 투명으로 바꾼다. 실제 알파 값은 FadeRoutine이 애니메이션한다.
    // 아웃라인 마스크 같은 커스텀 셰이더는 _Color/_BaseColor 자체가 없을 수 있어 원본을 그대로 돌려준다
    // (Material.color가 그런 셰이더에서 에러 로그를 남기므로 HasProperty로 먼저 걸러야 한다).
    private static Material MakeTransparentClone(Material source)
    {
        if (!source.HasProperty("_BaseColor") && !source.HasProperty("_Color"))
            return source;

        var mat = new Material(source);

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

    private void OnDestroy()
    {
        if (_originalMaterials.Count > 0) Restore();
    }
}
