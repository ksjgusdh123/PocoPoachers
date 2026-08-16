using UnityEngine;
using UnityEngine.UI;

// 붙어 있는 UI 그래픽에 둥근 사각형 외곽선을 그린다. 본체가 반투명해도 외곽선은 물들지 않는다.
//
// 붙이는 곳: 외곽선을 두르고 싶은 Image / RawImage.
//
// uGUI의 Outline은 메시를 복제해 뒤에 깔기 때문에 반투명 본체 뒤로 외곽선 색이 비친다.
// 이쪽은 셰이더가 한 픽셀 안에서 본체와 외곽선을 나눠 그리므로 그 문제가 없다.
//
// 외곽선은 사각형 경계 바깥에 그려진다. RectTransform을 키우면 레이아웃이 틀어지므로
// RectTransform은 그대로 두고 메시에만 여백 띠 4개를 덧붙인다. 원본 정점의 위치는 건드리지
// 않아 9-slice나 Filled 이미지도 그대로 동작한다.
//
// Canvas는 배칭하면서 정점을 루트 캔버스 공간으로 구워 넣는다. 그래서 셰이더는 POSITION만으로
// 사각형 기준 좌표를 알 수 없다. 모든 정점의 uv1에 그 좌표를 실어 보내고, Canvas의
// Additional Shader Channels에 TexCoord1을 켜 둔다.
//
// 머티리얼은 IMaterialModifier로 갈아 끼운다. Mask가 쓰는 방식과 같아서 Image에 직렬화되지 않는다.
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform))]
public class UIRoundedOutline : BaseMeshEffect, IMaterialModifier
{
    // 두께 바깥으로 안티에일리어싱과 반올림 오차가 걸칠 만큼만 더 넓힌다.
    private const float PaddingMargin = 2f;

    // 번짐은 지수 감쇠라 끝이 없다. 폭의 3배면 5% 아래로 떨어져 잘린 티가 안 난다.
    private const float GlowTailFactor = 3f;

    private const string FallbackMaterialPath = "M_UIRoundedOutline";

    private static readonly int RectSizeId = Shader.PropertyToID("_RectSize");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int ThicknessId = Shader.PropertyToID("_Thickness");
    private static readonly int BodyAlphaId = Shader.PropertyToID("_BodyAlpha");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int GlowColorId = Shader.PropertyToID("_GlowColor");
    private static readonly int GlowWidthId = Shader.PropertyToID("_GlowWidth");
    private static readonly int GlowIntensityId = Shader.PropertyToID("_GlowIntensity");
    private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
    private static readonly int PulseAmountId = Shader.PropertyToID("_PulseAmount");

    [SerializeField, Tooltip("비워두면 Resources의 M_UIRoundedOutline을 쓴다.")]
    private Material _outlineMaterial;

    [Header("Outline")]
    [SerializeField, Tooltip("외곽선 색. 알파를 1로 두면 본체가 반투명해도 선명하게 남는다.")]
    private Color _outlineColor = new Color32(0x4F, 0xD8, 0xF5, 0xFF);

    [SerializeField, Min(0f), Tooltip("외곽선 두께(px). 경계에서 바깥쪽으로 그려진다.")]
    private float _thickness = 2f;

    [Header("Glow")]
    [SerializeField, Tooltip("외곽선 바깥으로 번지는 빛의 색.")]
    private Color _glowColor = new Color32(0x4F, 0xD8, 0xF5, 0xFF);

    [SerializeField, Min(0f), Tooltip("번짐 폭(px). 이 값에 비례해 메시 여백이 늘어난다.")]
    private float _glowWidth = 16f;

    [SerializeField, Min(0f), Tooltip("발광 세기. 0이면 야광을 끈다. 가산이라 올릴수록 흰색으로 포화된다.")]
    private float _glowIntensity = 1f;

    [SerializeField, Min(0f), Tooltip("맥동 속도. 0이면 일정하게 빛난다. 2를 넘으면 경고등처럼 보인다.")]
    private float _pulseSpeed = 0f;

    [SerializeField, Range(0f, 1f), Tooltip("맥동으로 어두워지는 폭.")]
    private float _pulseAmount = 0.25f;

    [Header("Body")]
    [SerializeField, Range(0f, 1f), Tooltip("본체 불투명도. 외곽선과 무관하게 조절된다.")]
    private float _bodyAlpha = 0.6f;

    [Header("Shape")]
    [SerializeField, Min(0f), Tooltip("모서리 둥글기(px). 본체와 외곽선에 함께 적용된다.")]
    private float _cornerRadius = 12f;

    private Material _instance;

    // 번짐이 있으면 그 꼬리까지 덮을 만큼 메시를 넓혀야 한다. 모자라면 번짐이 사각형으로 잘린다.
    private float Padding => _thickness + PaddingMargin
                           + (_glowIntensity > 0f ? _glowWidth * GlowTailFactor : 0f);

    protected override void OnEnable()
    {
        base.OnEnable();
        if (graphic != null) graphic.SetMaterialDirty();
    }

    protected override void OnDisable()
    {
        // 비활성화되면 GetModifiedMaterial이 원본을 그대로 돌려주므로 다시 물어보게만 하면 된다.
        if (graphic != null) graphic.SetMaterialDirty();
        base.OnDisable();
    }

    protected override void OnDestroy()
    {
        DestroyInstance();
        base.OnDestroy();
    }

    protected override void OnRectTransformDimensionsChange()
    {
        base.OnRectTransformDimensionsChange();
        PushRect();
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();

        if (graphic == null) return;
        graphic.SetVerticesDirty();
        graphic.SetMaterialDirty();
    }
#endif

    public virtual Material GetModifiedMaterial(Material baseMaterial)
    {
        if (!IsActive()) return baseMaterial;

        EnsureInstance();
        if (_instance == null) return baseMaterial;

        PushSettings();
        return _instance;
    }

    public override void ModifyMesh(VertexHelper vh)
    {
        if (!IsActive()) return;

        EnsureShaderChannels();

        Rect r = ((RectTransform)transform).rect;
        Color32 tint = GetTint(vh);
        float p = Padding;

        // 위/아래 띠는 좌우 여백까지 덮고, 좌/우 띠는 원래 높이만 덮는다. 겹치지 않고 링이 채워진다.
        AddQuad(vh, r.xMin - p, r.yMax, r.xMax + p, r.yMax + p, tint);
        AddQuad(vh, r.xMin - p, r.yMin - p, r.xMax + p, r.yMin, tint);
        AddQuad(vh, r.xMin - p, r.yMin, r.xMin, r.yMax, tint);
        AddQuad(vh, r.xMax, r.yMin, r.xMax + p, r.yMax, tint);

        WriteLocalCoords(vh, r.center);
        PushRect();
    }

    // 덧붙인 띠까지 포함해 모든 정점에 사각형 중심 기준 좌표를 싣는다. 셰이더는 이걸로 거리를 잰다.
    private static void WriteLocalCoords(VertexHelper vh, Vector2 center)
    {
        UIVertex vertex = default;

        for (int i = 0; i < vh.currentVertCount; i++)
        {
            vh.PopulateUIVertex(ref vertex, i);
            vertex.uv1 = new Vector4(vertex.position.x - center.x, vertex.position.y - center.y, 0f, 0f);
            vh.SetUIVertex(vertex, i);
        }
    }

    // TexCoord1이 꺼져 있으면 uv1이 0으로 들어와 거리장이 한 점으로 무너진다.
    private void EnsureShaderChannels()
    {
        Canvas canvas = graphic != null ? graphic.canvas : null;
        if (canvas == null) return;

        const AdditionalCanvasShaderChannels Required = AdditionalCanvasShaderChannels.TexCoord1;
        if ((canvas.additionalShaderChannels & Required) == Required) return;

        canvas.additionalShaderChannels |= Required;
    }

    // 덧붙인 띠도 원본과 같은 정점 색을 써야 CanvasGroup 페이드가 함께 먹는다.
    private Color32 GetTint(VertexHelper vh)
    {
        if (vh.currentVertCount == 0) return graphic != null ? (Color32)graphic.color : (Color32)Color.white;

        UIVertex vertex = default;
        vh.PopulateUIVertex(ref vertex, 0);
        return vertex.color;
    }

    private static void AddQuad(VertexHelper vh, float xMin, float yMin, float xMax, float yMax, Color32 color)
    {
        int index = vh.currentVertCount;

        UIVertex vertex = UIVertex.simpleVert;
        vertex.color = color;

        vertex.position = new Vector3(xMin, yMin);
        vh.AddVert(vertex);
        vertex.position = new Vector3(xMin, yMax);
        vh.AddVert(vertex);
        vertex.position = new Vector3(xMax, yMax);
        vh.AddVert(vertex);
        vertex.position = new Vector3(xMax, yMin);
        vh.AddVert(vertex);

        vh.AddTriangle(index, index + 1, index + 2);
        vh.AddTriangle(index + 2, index + 3, index);
    }

    private void EnsureInstance()
    {
        if (_instance != null) return;

        Material source = _outlineMaterial != null ? _outlineMaterial : Resources.Load<Material>(FallbackMaterialPath);
        if (source == null)
        {
            Debug.LogWarning($"[{nameof(UIRoundedOutline)}] 외곽선 머티리얼을 찾지 못했습니다. Resources/{FallbackMaterialPath}가 있는지 확인하세요.", this);
            return;
        }

        // 패널마다 크기가 달라 인스턴스가 필요하다. 씬에 저장되면 안 되므로 DontSave로 둔다.
        _instance = new Material(source)
        {
            name = $"{source.name} (Instance)",
            hideFlags = HideFlags.HideAndDontSave,
        };
    }

    private void DestroyInstance()
    {
        if (_instance == null) return;

        if (Application.isPlaying) Destroy(_instance);
        else DestroyImmediate(_instance);

        _instance = null;
    }

    private void PushSettings()
    {
        if (_instance == null) return;

        _instance.SetColor(OutlineColorId, _outlineColor);
        _instance.SetFloat(ThicknessId, _thickness);
        _instance.SetFloat(BodyAlphaId, _bodyAlpha);
        _instance.SetFloat(RadiusId, _cornerRadius);

        _instance.SetColor(GlowColorId, _glowColor);
        _instance.SetFloat(GlowWidthId, _glowWidth);
        _instance.SetFloat(GlowIntensityId, _glowIntensity);
        _instance.SetFloat(PulseSpeedId, _pulseSpeed);
        _instance.SetFloat(PulseAmountId, _pulseAmount);

        PushRect();
    }

    private void PushRect()
    {
        if (_instance == null) return;

        _instance.SetVector(RectSizeId, ((RectTransform)transform).rect.size);
    }
}
