using UnityEngine;
using UnityEngine.UI;

// UI 패널 뒤에 실시간 블러 화면을 깐다.
//
// 붙이는 곳: 블러를 깔고 싶은 패널의 루트. 첫 자식으로 배경 RawImage를 자동 생성한다.
//            (RawImage가 이미 붙어 있는 오브젝트에 두면 그것을 그대로 쓴다)
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

    [SerializeField, Tooltip("M_UIBlurBackdrop")]
    private Material _backdropMaterial;

    [SerializeField, Tooltip("블러에 곱할 색. 어둡게 깔려면 RGB를 낮춘다. 알파는 배경 전체의 불투명도.")]
    private Color _tint = new Color(0.35f, 0.38f, 0.45f, 1f);

    [SerializeField, Tooltip("체크하면 배경이 뒤쪽 클릭을 막는다.")]
    private bool _blockRaycast = true;

    private RawImage _image;

    private void Awake() => EnsureBackdrop();

    private void OnEnable()
    {
        EnsureBackdrop();
        _activeCount++;
    }

    private void OnDisable() => _activeCount = Mathf.Max(0, _activeCount - 1);

    private void EnsureBackdrop()
    {
        if (_image != null) return;

        if (_backdropMaterial == null)
            Debug.LogWarning($"[{nameof(UIRealtimeBackdropBlur)}] 배경 머티리얼이 연결되지 않았습니다.", this);

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
        _image.material = _backdropMaterial;
        _image.color = _tint;
        _image.raycastTarget = _blockRaycast;
    }
}
