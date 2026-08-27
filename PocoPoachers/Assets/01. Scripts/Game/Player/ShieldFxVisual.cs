using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 방어막류 스킬(무적/반사 등)의 셰이더 프리팹 — 스폰/페이드/네트워크 상태 반영을 모두 담당한다.
// 프리팹 경로별로 별개 인스턴스를 관리하므로, 같은 플레이어가 무적+반사를 동시에 켜도 각자 따로 뜬다.
// CombatDrone과 같은 방식으로 소유자 playerId별로 관리한다: 본인은 각 스킬이 SpawnSelf로 즉시 붙이고,
// 다른 플레이어 몫은 각 스킬 전용 H_/G_ 패킷 수신 시 SetActiveFor로 그 플레이어 오브젝트 옆에 띄운다.
//
// Hologram_Shader.shadergraph를 추적해보면 중앙 투명/테두리 발광 형태는 _Opacity_On_Centre가 낀
// 프레넬 체인이 만드는 셰이프(알파 마스크)이고, _Color_A/_Texture_1_Color는 그 위에 얹히는 색상 밝기다.
// _Opacity_On_Centre를 직접 페이드시키면 셰이프 자체가 바뀌어 중앙까지 불투명한 통짜 구가 되어버린다
// (실제로 확인됨) — 그래서 셰이프는 원본 그대로 두고, 색상 밝기(RGB)만 0↔원본으로 스케일한다.
// 가산(Additive) 블렌드라 밝기가 0이면 사실상 안 보이는 것과 같아 페이드처럼 보인다.
// 이 스케일 방식은 Shield Shader FREE 팩의 모든 프리팹이 같은 프로퍼티명을 쓰는 한(FREE_1/FREE_4 확인됨)
// 프리팹이 바뀌어도 그대로 통한다.
public class ShieldFxVisual : MonoBehaviour
{
    private const float HeightOffset = 0.3f; // 캐릭터 몸통 중심쯤에 방어막을 띄운다

    private static readonly string[] ColorProperties = { "_Color_A", "_Texture_1_Color" };
    private const float FadeDuration = 0.3f;

    // (소유자 playerId, 프리팹 경로) → 인스턴스. 원격 플레이어 몫만 등록한다(본인 것은 스킬이 직접 들고 있음).
    private static readonly Dictionary<(int playerId, string prefabPath), ShieldFxVisual> ByOwner = new();
    private int _ownerPlayerId = -1;
    private string _prefabPath;

    // 방어막 콜라이더 전용 레이어 — 물리 충돌 매트릭스(ProjectSettings)에서 아무 레이어와도 안 부딪히게
    // 꺼놨다(총알 스윕만 감지되고, 밀림/이동 차단 같은 일반 물리 반응은 전혀 없음).
    // Bullet.cs의 _hitMask에도 이 레이어를 포함시켜야 한다(총알 프리팹 4종에 이미 반영됨).
    private const string ShieldLayerName = "Shield";

    // 본인 스폰 — 각 스킬의 Begin에서 호출. 반환값을 들고 있다가 종료 시 FadeOutAndDestroy를 부른다.
    public static ShieldFxVisual SpawnSelf(Transform owner, StatBase ownerStat, string prefabPath) => Spawn(owner, ownerStat, prefabPath);

    // 다른 플레이어의 방어막 상태 반영 — 스킬별 G_/H_ 핸들러가 자기 프리팹 경로로 호출한다.
    public static void SetActiveFor(int playerId, string prefabPath, bool active)
    {
        var key = (playerId, prefabPath);
        var existing = FindFor(key);

        if (!active)
        {
            existing?.FadeOutAndDestroy();
            return;
        }

        if (existing != null) return; // 이미 떠 있음

        var om = ObjectManager.Instance;
        if (om == null || !om.TryGet(ObjectKind.Player, playerId, out var playerObj)) return;
        if (!playerObj.gameObject.TryGetComponent<StatBase>(out var ownerStat)) return;

        var visual = Spawn(playerObj.transform, ownerStat, prefabPath);
        if (visual == null) return;

        visual._ownerPlayerId = playerId;
        visual._prefabPath = prefabPath;
        ByOwner[key] = visual;
    }

    private static ShieldFxVisual Spawn(Transform owner, StatBase ownerStat, string prefabPath)
    {
        var prefab = Resources.Load<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[ShieldFxVisual] 방어막 프리팹을 찾을 수 없습니다: Resources/{prefabPath}");
            return null;
        }

        var instance = Instantiate(prefab, owner);
        instance.transform.localPosition = Vector3.up * HeightOffset;
        instance.transform.localRotation = Quaternion.identity;

        // 콜라이더는 끄지 않고 살려서 총알이 (몸이 아니라) 방어막 표면에서 바로 막히거나 반사되게 한다.
        // 전용 Shield 레이어로 옮겨 일반 물리 충돌(밀림 등)에서는 완전히 빠지고, 총알 스윕에만 걸리게 한다.
        int shieldLayer = LayerMask.NameToLayer(ShieldLayerName);
        foreach (var col in instance.GetComponentsInChildren<Collider>())
        {
            if (shieldLayer >= 0) col.gameObject.layer = shieldLayer;
            var link = col.gameObject.AddComponent<ShieldHitboxLink>();
            link.Owner = ownerStat;
        }

        return instance.AddComponent<ShieldFxVisual>();
    }

    private static ShieldFxVisual FindFor((int playerId, string prefabPath) key)
    {
        if (!ByOwner.TryGetValue(key, out var visual)) return null;
        if (visual != null) return visual;

        ByOwner.Remove(key); // 씬 전환 등으로 파괴된 항목 정리
        return null;
    }

    private void Awake()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            // .materials는 접근 시점에 렌더러별 인스턴스 클론을 만들어준다 — 원본 에셋/다른 인스턴스에 영향 없음
            foreach (var mat in renderer.materials)
            {
                foreach (var prop in ColorProperties)
                {
                    if (mat.HasProperty(prop))
                        _colors.Add((mat, prop, mat.GetColor(prop)));
                }
            }
        }

        SetBrightness(0f); // 스폰 첫 프레임에 반짝 나타나지 않도록 0에서 시작
    }

    private void OnEnable() => FadeTo(1f, null);

    // 스킬 종료/원격 종료 통보 시 밖에서 호출 — 다 사라진 뒤 스스로 파괴한다.
    public void FadeOutAndDestroy() => FadeTo(0f, () => Destroy(gameObject));

    private void OnDestroy()
    {
        if (_ownerPlayerId < 0 || _prefabPath == null) return;

        var key = (_ownerPlayerId, _prefabPath);
        if (ByOwner.TryGetValue(key, out var v) && v == this)
            ByOwner.Remove(key);
    }

    private readonly List<(Material mat, string prop, Color original)> _colors = new();
    private Coroutine _fadeRoutine;
    private float _currentT;

    private void FadeTo(float target, Action onComplete)
    {
        if (_colors.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        if (_fadeRoutine != null) StopCoroutine(_fadeRoutine);
        _fadeRoutine = StartCoroutine(FadeRoutine(target, onComplete));
    }

    private IEnumerator FadeRoutine(float target, Action onComplete)
    {
        float start = _currentT;
        float t = 0f;
        while (t < FadeDuration)
        {
            t += Time.deltaTime;
            SetBrightness(Mathf.Lerp(start, target, t / FadeDuration));
            yield return null;
        }
        SetBrightness(target);
        _fadeRoutine = null;
        onComplete?.Invoke();
    }

    private void SetBrightness(float t)
    {
        _currentT = t;
        foreach (var (mat, prop, original) in _colors)
            mat.SetColor(prop, original * t);
    }
}
