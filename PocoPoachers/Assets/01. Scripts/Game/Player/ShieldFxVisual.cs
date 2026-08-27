using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// InvincibleSkill이 붙이는 방어막(ShieldFX) 프리팹의 등장/소멸 페이드.
// Hologram_Shader.shadergraph를 추적해보면 중앙 투명/테두리 발광 형태는 _Opacity_On_Centre가 낀
// 프레넬 체인이 만드는 셰이프(알파 마스크)이고, _Color_A/_Texture_1_Color는 그 위에 얹히는 색상 밝기다.
// _Opacity_On_Centre를 직접 페이드시키면 셰이프 자체가 바뀌어 중앙까지 불투명한 통짜 구가 되어버린다
// (실제로 확인됨) — 그래서 셰이프는 원본 그대로 두고, 색상 밝기(RGB)만 0↔원본으로 스케일한다.
// 가산(Additive) 블렌드라 밝기가 0이면 사실상 안 보이는 것과 같아 페이드처럼 보인다.
public class ShieldFxVisual : MonoBehaviour
{
    private static readonly string[] ColorProperties = { "_Color_A", "_Texture_1_Color" };
    private const float FadeDuration = 0.3f;

    private readonly List<(Material mat, string prop, Color original)> _colors = new();
    private Coroutine _fadeRoutine;
    private float _currentT;

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

    // 스킬 종료 시 밖에서 호출 — 다 사라진 뒤 스스로 파괴한다.
    public void FadeOutAndDestroy() => FadeTo(0f, () => Destroy(gameObject));

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
