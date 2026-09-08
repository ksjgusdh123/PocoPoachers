using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 현재 씬에 맞는 미니맵 이미지 · 좌표 데이터 · 가장자리 스타일을 한꺼번에 갈아끼운다.
// 셋이 따로 놀면 마커가 엉뚱한 곳에 찍히므로 반드시 같은 MinimapCaptureData에서 함께 가져온다.
[RequireComponent(typeof(MinimapMarkerSpawner))]
public class MinimapMapBinder : MonoBehaviour
{
    [SerializeField] private Image _mapImage;
    [SerializeField] private MinimapMarkerSpawner _markerSpawner;
    [SerializeField] private MinimapCaptureData _fallback; // 카탈로그에 현재 씬이 없을 때 쓴다

    private static readonly int CenterXId = Shader.PropertyToID("_CenterX");
    private static readonly int CenterYId = Shader.PropertyToID("_CenterY");
    private static readonly int RadiusXId = Shader.PropertyToID("_RadiusX");
    private static readonly int RadiusYId = Shader.PropertyToID("_RadiusY");
    private static readonly int SoftnessId = Shader.PropertyToID("_Softness");
    private static readonly int WobbleAmountId = Shader.PropertyToID("_WobbleAmount");
    private static readonly int WobbleFrequencyId = Shader.PropertyToID("_WobbleFrequency");
    private static readonly int WobbleSeedId = Shader.PropertyToID("_WobbleSeed");
    private static readonly int MaskTexId = Shader.PropertyToID("_MaskTex");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int OutlineSharpnessId = Shader.PropertyToID("_OutlineSharpness");
    private static readonly int OutlineGlowId = Shader.PropertyToID("_OutlineGlow");

    private Material _materialInstance;

    private void Awake()
    {
        if (_markerSpawner == null) _markerSpawner = GetComponent<MinimapMarkerSpawner>();

        string sceneName = SceneManager.GetActiveScene().name;
        MinimapCaptureData data = MinimapCatalog.For(sceneName) ?? _fallback;

        if (data == null)
        {
            Debug.LogWarning($"[MinimapMapBinder] '{sceneName}'의 미니맵 데이터를 찾지 못했습니다. Tools > Generator > Minimap Capture로 촬영하세요.");
            return;
        }

        Bind(data);
    }

    public void Bind(MinimapCaptureData data)
    {
        if (data == null) return;

        // 마커는 미니맵이 열릴 때 생성되므로 Awake 시점에 넣어두면 늦지 않는다
        _markerSpawner?.SetMapData(data);

        if (_mapImage == null) return;

        if (data.MinimapSprite != null) _mapImage.sprite = data.MinimapSprite;
        else Debug.LogWarning($"[MinimapMapBinder] {data.name}에 MinimapSprite가 없습니다. PNG의 Texture Type이 Sprite인지 확인하세요.");

        ApplyEdgeStyle(data.EdgeStyle);
    }

    // Image.material은 에셋 원본을 그대로 돌려준다 — 여기에 값을 쓰면 프로젝트의 .mat 파일이 실제로 수정되므로
    // 반드시 복제본을 만들어 그쪽에만 쓴다.
    private void ApplyEdgeStyle(MinimapEdgeStyle style)
    {
        if (style == null || _mapImage.material == null) return;

        if (_materialInstance == null)
        {
            _materialInstance = new Material(_mapImage.material) { name = _mapImage.material.name + " (Instance)" };
            _mapImage.material = _materialInstance;
        }

        _materialInstance.SetFloat(CenterXId, style.CenterX);
        _materialInstance.SetFloat(CenterYId, style.CenterY);
        _materialInstance.SetFloat(RadiusXId, style.RadiusX);
        _materialInstance.SetFloat(RadiusYId, style.RadiusY);
        _materialInstance.SetFloat(SoftnessId, style.Softness);

        _materialInstance.SetFloat(WobbleAmountId, style.WobbleAmount);
        _materialInstance.SetFloat(WobbleFrequencyId, style.WobbleFrequency);
        _materialInstance.SetFloat(WobbleSeedId, style.WobbleSeed);

        // 비워두면 셰이더 기본값(흰색 = 전체 보임)이 그대로 남는다
        if (style.Mask != null) _materialInstance.SetTexture(MaskTexId, style.Mask);

        _materialInstance.SetColor(OutlineColorId, style.OutlineColor);
        _materialInstance.SetFloat(OutlineSharpnessId, style.OutlineSharpness);
        _materialInstance.SetFloat(OutlineGlowId, style.OutlineGlow);
    }

    private void OnDestroy()
    {
        if (_materialInstance != null) Destroy(_materialInstance);
    }
}
