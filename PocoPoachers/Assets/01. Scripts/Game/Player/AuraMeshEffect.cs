using System.Collections.Generic;
using UnityEngine;

// 팀원 버프 오라 연출 — 캐릭터 본체 SkinnedMeshRenderer의 머터리얼 슬롯에 오라 머터리얼을 추가/제거한다
// (별도 오브젝트를 덧씌우는 ShieldFxVisual과 달리, 캐릭터 메쉬 자체에 얹는 방식). 오라 셰이더가 Transparent로
// 설정돼 있어야 기존 스킨 위에 자연스럽게 겹쳐 보인다(HitRimEffect와 같은 "머터리얼 슬롯 추가" 기법).
// 여러 버프(공격력/방어력/이동속도)가 동시에 걸릴 수 있어 머터리얼별로 독립적으로 추가/제거한다.
public class AuraMeshEffect : MonoBehaviour
{
    private static readonly Dictionary<string, Material> MaterialCache = new();

    private SkinnedMeshRenderer[] _smrs;
    private readonly HashSet<Material> _activeMaterials = new();

    private void Awake()
    {
        _smrs = GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    public void SetActive(string materialResourcePath, bool active)
    {
        Material mat = GetOrLoadMaterial(materialResourcePath);
        if (mat == null) return;

        bool alreadyActive = _activeMaterials.Contains(mat);
        if (alreadyActive == active) return;

        if (active) _activeMaterials.Add(mat);
        else _activeMaterials.Remove(mat);

        foreach (var smr in _smrs)
        {
            var mats = new List<Material>(smr.sharedMaterials);
            mats.RemoveAll(m => m == mat); // 항상 지운 뒤
            if (active) mats.Add(mat);      // 켜는 거면 다시 붙인다 (중복 슬롯 방지)
            smr.materials = mats.ToArray();
        }
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
