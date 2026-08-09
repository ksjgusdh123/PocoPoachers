using UnityEngine;
using UnityEngine.UI;

// UI 패널 뒤에 실시간 블러 화면을 깐다.
//
// 붙이는 곳: 블러를 깔고 싶은 RectTransform. 첫 자식으로 배경 RawImage를 자동 생성한다.
//            UIType이나 UIBase와 무관하므로, 타입이 없는 중간 패널에도 그냥 붙이면 된다.
//            (RawImage가 이미 붙어 있는 오브젝트에 두면 그것을 그대로 쓴다)
//
// UIBase를 상속한 패널은 Awake에서 자동으로 붙는다. 컨테이너 안쪽처럼 자동으로 잡히지 않는
// 위치는 직접 Add Component 하면 된다.
//
// 블러 자체는 UIBlurFeature가 만드는 전역 _UIBlurTex에서 온다. 이 컴포넌트는 배경을 배치하고
// 켜져 있는 개수를 세어 Feature를 켜고 끄는 스위치 역할만 한다. 하나도 없으면 블러 패스가 아예 빠진다.
[DisallowMultipleComponent]
public class UIRealtimeBackdropBlur : MonoBehaviour
{
    private const string BackdropName = "BlurBackdrop";

    private static int _activeCount;

    public static bool AnyActive => _activeCount > 0;

    // 도메인 리로드를 끈 상태에서 Play를 반복하면 카운트가 남아 Feature가 계속 돈다.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCount() => _activeCount = 0;

    // UIBase의 자동 부착 경로. 이미 붙어 있으면 그대로 둔다(인스펙터 설정을 덮어쓰지 않는다).
    public static void Attach(GameObject target)
    {
        if (target == null) return;
        if (target.GetComponent<UIRealtimeBackdropBlur>() != null) return;

        target.AddComponent<UIRealtimeBackdropBlur>();
    }

    [SerializeField, Tooltip("비워두면 UITheme의 BackdropBlurMaterial을 쓴다.")]
    private Material _backdropMaterial;

    [SerializeField, Tooltip("체크 해제하면 아래 Tint 값을 쓰고, 체크하면 UITheme의 색을 따른다.")]
    private bool _useThemeTint = true;

    [SerializeField, Tooltip("블러에 곱할 색. 어둡게 깔려면 RGB를 낮춘다. 알파는 배경 전체의 불투명도.")]
    private Color _tint = new Color(0.35f, 0.38f, 0.45f, 1f);

    [SerializeField, Tooltip("체크하면 배경이 뒤쪽 클릭을 막는다.")]
    private bool _blockRaycast = true;

    private RawImage _image;

    private void Awake() => EnsureBackdrop();

    private void OnEnable()
    {
        EnsureBackdrop();

        // 테마 색을 바꾸면 다음에 열 때 반영되도록 열 때마다 다시 적용한다.
        if (_image != null) ApplySettings();

        _activeCount++;
    }

    private void OnDisable() => _activeCount = Mathf.Max(0, _activeCount - 1);

    private void EnsureBackdrop()
    {
        if (_image != null) return;

        // RawImage 위에 직접 올라가 있으면 그것을 배경으로 쓴다.
        if (TryGetComponent(out _image))
        {
            ApplySettings();
            return;
        }

        Transform existing = transform.Find(BackdropName);
        if (existing != null) _image = existing.GetComponent<RawImage>();

        if (_image == null)
        {
            var go = new GameObject(BackdropName, typeof(RectTransform), typeof(RawImage));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            _image = go.GetComponent<RawImage>();
        }

        // LayoutGroup이 붙은 패널(예: DescriptionUI 계열)은 자식의 위치·크기를 매 프레임 덮어쓴다.
        // 배경은 레이아웃에서 빼야 아래에서 잡는 스트레치가 유지되고, ContentSizeFitter 계산도 오염시키지 않는다.
        if (!_image.TryGetComponent(out LayoutElement layoutElement))
            layoutElement = _image.gameObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        var rect = (RectTransform)_image.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.SetAsFirstSibling();

        ApplySettings();
    }

    private void ApplySettings()
    {
        var theme = UITheme.Default;

        Material material = _backdropMaterial != null ? _backdropMaterial : theme != null ? theme.BackdropBlurMaterial : null;
        if (material == null)
            Debug.LogWarning($"[{nameof(UIRealtimeBackdropBlur)}] 배경 머티리얼이 없습니다. UITheme의 BackdropBlurMaterial을 채우세요.", this);

        _image.material = material;
        _image.color = _useThemeTint && theme != null ? theme.BackdropBlurTint : _tint;
        _image.raycastTarget = _blockRaycast;
    }
}
