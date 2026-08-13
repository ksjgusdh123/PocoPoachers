using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 눈을 뜨는 연출 — 완전한 암전에서 시작해 몇 번 껌뻑이다가 서서히 또렷해진다.
//
// 화면을 두 겹으로 덮는다. 어둠과 흐림을 한 겹으로 하면 검은색이 블러를 덮어버려
// "흐릿하게 보이는" 게 아니라 그냥 어두워지기만 한다.
//   1) 블러 겹 — UI 패널 뒤에 쓰는 실시간 블러(_UIBlurTex)를 화면 전체에 깐다. 알파가 1이면 완전히 흐릿
//   2) 암전 겹 — 그 위에 덮는 검정. 알파가 1이면 아무것도 안 보임
// 껌뻑일 때는 암전만 걷었다 덮어서, 눈을 뜬 순간에는 흐릿한 화면이 보이게 한다.
//
// 씬 이동 쪽에서 PlayOnSceneLoaded(목적지, 시간)만 예약해두면 그 씬이 로드될 때 알아서 재생된다.
public class ScreenWakeUp : MonoBehaviour
{
    private const float BlackHold = 0.4f;   // 처음 완전 암전을 유지하는 시간
    private const float OpenHold = 0.12f;   // 뜬 채로 머무는 시간

    private struct Blink
    {
        public float darkness;  // 이 회차에 암전이 걷히는 정도 (0이면 완전히 밝음)
        public float openTime;  // 눈을 뜨는 데 걸리는 시간
        public float closeTime; // 다시 감기는 시간
        public bool linear;     // true면 일정한 속도, false면 느리게 시작해 느리게 끝남
    }

    // 1회차는 일정한 속도로 떴다 감고, 마지막 회차는 천천히 뜬 뒤 그대로 이어진다(감지 않는다)
    private static readonly Blink[] Blinks =
    {
        new Blink { darkness = 0.5f,  openTime = 1f, closeTime = 1f, linear = true },
        new Blink { darkness = 0.25f, openTime = 1.5f, closeTime = 0f, linear = false },
    };

    private static string _pendingScene;
    private static float _pendingDuration;

    private RawImage _blur;
    private Image _dark;

    // 로딩 화면을 거치면 sceneLoaded가 여러 번 오므로 목적지 이름이 맞을 때만 재생한다
    public static void PlayOnSceneLoaded(string sceneName, float duration)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        _pendingScene = sceneName;
        _pendingDuration = duration;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != _pendingScene) return;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        _pendingScene = null;

        Play(_pendingDuration);
    }

    public static void Play(float duration)
    {
        var runner = new GameObject(nameof(ScreenWakeUp)).AddComponent<ScreenWakeUp>();
        runner.StartCoroutine(runner.Routine(duration));
    }

    private IEnumerator Routine(float duration)
    {
        CreateOverlay();
        SetBlur(1f);
        SetDark(1f);

        yield return new WaitForSeconds(BlackHold);

        // 껌뻑임 — 블러는 그대로 두고 암전만 여닫는다.
        // 마지막 회차는 뜬 채로 두고 그대로 최종 개안으로 이어진다.
        for (int i = 0; i < Blinks.Length; i++)
        {
            Blink blink = Blinks[i];
            yield return FadeDark(blink.darkness, blink.openTime, blink.linear);

            if (i == Blinks.Length - 1) break;

            yield return new WaitForSeconds(OpenHold);
            yield return FadeDark(1f, blink.closeTime, blink.linear);
        }

        // 최종 개안 — 남은 어둠이 먼저 걷히고, 흐릿함은 더 오래 남았다가 사라진다
        float startDark = _dark.color.a;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            SetDark(startDark * Mathf.Pow(1f - t, 3f));
            SetBlur(Mathf.Pow(1f - t, 1.2f));

            yield return null;
        }

        Destroy(gameObject);
    }

    private IEnumerator FadeDark(float target, float time, bool linear)
    {
        float from = _dark.color.a;
        float elapsed = 0f;

        while (elapsed < time)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / time;
            SetDark(Mathf.Lerp(from, target, linear ? t : Mathf.SmoothStep(0f, 1f, t)));
            yield return null;
        }

        SetDark(target);
    }

    private void SetDark(float alpha) => SetAlpha(_dark, alpha);

    private void SetBlur(float alpha)
    {
        if (_blur != null) SetAlpha(_blur, alpha);
    }

    private static void SetAlpha(Graphic graphic, float alpha)
    {
        Color color = graphic.color;
        color.a = alpha;
        graphic.color = color;
    }

    private void CreateOverlay()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 30000; // 다른 UI보다 확실히 위

        // 블러 겹 — 머티리얼이 없으면(테마 미설정) 이 겹은 만들지 않고 암전만으로 진행한다
        Material blurMaterial = UITheme.Default != null ? UITheme.Default.BackdropBlurMaterial : null;
        if (blurMaterial != null)
        {
            var blurObject = CreateFullScreenChild("Blur", typeof(RawImage));
            _blur = blurObject.GetComponent<RawImage>();

            // UIRealtimeBackdropBlur는 붙은 오브젝트의 RawImage를 배경으로 쓴다.
            // 이게 켜져 있어야 블러 패스(UIBlurFeature) 자체가 돌아간다.
            blurObject.AddComponent<UIRealtimeBackdropBlur>();

            // 위 컴포넌트가 테마 색/머티리얼을 덮어쓰므로 그다음에 우리 값으로 되돌린다.
            // 흰색이어야 블러된 화면이 그대로 보인다 — 검게 두면 블러가 안 보이고 그냥 어두워진다.
            _blur.material = blurMaterial;
            _blur.color = Color.white;
            _blur.raycastTarget = false;
        }

        var darkObject = CreateFullScreenChild("Dark", typeof(Image));
        _dark = darkObject.GetComponent<Image>();
        _dark.color = Color.black;
        _dark.raycastTarget = false;
    }

    private GameObject CreateFullScreenChild(string name, System.Type graphicType)
    {
        var go = new GameObject(name, typeof(RectTransform), graphicType);
        go.transform.SetParent(transform, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return go;
    }
}
