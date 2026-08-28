using System.Collections.Generic;
using UnityEngine;

// 팀원 버프 오라 연출 — 캐릭터 본체 SkinnedMeshRenderer에 오라 머터리얼 슬롯을 항상 1개만 유지하고,
// 동시에 걸린 버프들의 색을 합쳐서(채널별 최댓값) 그 슬롯 하나에 반영한다.
//
// 원래는 버프마다 별개 머터리얼(Attack/Defense/Speed)을 각각 슬롯에 추가하는 방식이었는데, 캐릭터
// 파츠마다 실제 서브메쉬 개수가 달라서(팔다리·몸통은 2개, 머리는 M_AtlasEmissive가 하나 더 있어 3개)
// 문제가 생겼다 — 슬롯 개수가 서브메쉬 개수를 넘어가면 Unity가 "넘치는 만큼 마지막 서브메쉬를 반복해서
// 그리는" 방식으로 처리하는데, 그 마지막 서브메쉬가 파츠마다 다른 지오메트리를 가리키다 보니(팔다리는
// 일반 스킨 영역, 머리는 이미시브 전용 영역) 같은 코드가 파츠마다 다르게 보였다. 슬롯을 항상 최대 1개만
// 쓰면(HitRimEffect와 같은 방식) 서브메쉬 개수 차이와 무관하게 항상 똑같이 동작한다.
public class AuraMeshEffect : MonoBehaviour
{
    private static readonly int GlowColorID = Shader.PropertyToID("_glow_color");
    private static readonly Dictionary<string, Material> MaterialCache = new();

    private SkinnedMeshRenderer[] _smrs;
    private Material _instance; // 이 플레이어 전용 복제본 — 슬롯 자체는 이것 하나만 붙였다 뗐다 한다
    private readonly Dictionary<string, Color> _activeColors = new();
    private bool _slotAttached;

    private void Awake()
    {
        _smrs = GetComponentsInChildren<SkinnedMeshRenderer>(true);
    }

    private void OnDestroy()
    {
        if (_instance != null) Destroy(_instance);
    }

    public void SetActive(string materialResourcePath, bool active)
    {
        Material source = GetOrLoadMaterial(materialResourcePath);
        if (source == null) return;

        if (active)
        {
            _activeColors[materialResourcePath] = source.HasColor(GlowColorID) ? source.GetColor(GlowColorID) : Color.white;
            _instance ??= new Material(source) { name = "AuraMeshEffect Instance" };
        }
        else
        {
            _activeColors.Remove(materialResourcePath);
        }

        Apply();
    }

    private void Apply()
    {
        bool shouldShow = _activeColors.Count > 0;

        if (shouldShow)
        {
            Color combined = new Color(0f, 0f, 0f, 0f);
            foreach (var c in _activeColors.Values)
            {
                combined.r = Mathf.Max(combined.r, c.r);
                combined.g = Mathf.Max(combined.g, c.g);
                combined.b = Mathf.Max(combined.b, c.b);
                combined.a = Mathf.Max(combined.a, c.a);
            }
            _instance.SetColor(GlowColorID, combined);
        }

        if (shouldShow == _slotAttached) return; // 켜짐/꺼짐 상태 자체는 안 바뀜 — 색만 갱신하면 끝

        foreach (var smr in _smrs)
        {
            var mats = new List<Material>(smr.sharedMaterials);
            mats.RemoveAll(m => m == _instance);
            if (shouldShow) mats.Add(_instance);
            smr.materials = mats.ToArray();
        }

        _slotAttached = shouldShow;
    }

    private static Material GetOrLoadMaterial(string resourcePath)
    {
        if (MaterialCache.TryGetValue(resourcePath, out var cached))
            return cached;

        Material mat = Resources.Load<Material>(resourcePath);
        if (mat == null)
        {
            Debug.LogError($"[AuraMeshEffect] 오라 머터리얼을 찾을 수 없습니다: Resources/{resourcePath}");
            return null;
        }

        MaterialCache[resourcePath] = mat;
        return mat;
    }

    // 대상 플레이어 GameObject에 오라를 켜고/끈다 — 컴포넌트가 없으면 필요할 때(켤 때만) 붙인다.
    public static void SetActiveFor(GameObject target, string materialResourcePath, bool active)
    {
        if (target == null) return;

        var effect = target.GetComponent<AuraMeshEffect>();
        if (effect == null)
        {
            if (!active) return; // 끄는 요청인데 컴포넌트가 아직 없으면 할 일이 없다
            effect = target.AddComponent<AuraMeshEffect>();
        }

        effect.SetActive(materialResourcePath, active);
    }
}
