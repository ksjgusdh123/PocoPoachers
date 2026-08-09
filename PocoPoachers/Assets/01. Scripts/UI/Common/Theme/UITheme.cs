using UnityEngine;
using UnityEngine.UI;

// UI 디자인 토큰의 단일 출처.
// 프리팹마다 흩어져 있던 색상/타이포 값을 여기로 모아, ThemedButtonUI 등이 참조해 적용한다.
// Resources 폴더에 UITheme 이름으로 하나 두면 인스펙터 연결 없이 자동으로 로드된다.
[CreateAssetMenu(fileName = "UITheme", menuName = "PocoPoachers/UI Theme")]
public class UITheme : ScriptableObject
{
    private const string ResourcePath = "UITheme";

    private static UITheme _default;

    public static UITheme Default
    {
        get
        {
            if (_default == null) _default = Resources.Load<UITheme>(ResourcePath);
            return _default;
        }
    }

    public enum ButtonStyle
    {
        Primary,
        Secondary,
        Danger,
    }

    public enum TypographyRole
    {
        Caption,
        Body,
        Subtitle,
        Title,
        Display,
        Hero,
        Banner,
    }

    // 텍스트 잉크 색의 역할. None이면 ThemedTextUI가 색을 건드리지 않는다(기존 동작).
    public enum TextColorRole
    {
        None,
        Primary,
        Secondary,
        Muted,
        Accent,
        OnAccent,
        Positive,
        Negative,
        Warning,
    }

    [Header("Palette")]
    [Tooltip("강조색 — 게이지, 테두리, 활성 상태에 사용")]
    public Color Accent = new Color32(0x00, 0xE5, 0xFF, 0xFF);

    [Tooltip("패널 배경색")]
    public Color Surface = new Color32(0x0A, 0x0F, 0x1D, 0xFF);
    [Tooltip("Surface 위에 올라가는 카드/모달 배경 — 한 단계 밝은 표면")]
    public Color SurfaceRaised = new Color32(0x18, 0x20, 0x33, 0xFF);

    [Tooltip("모달 뒤를 덮는 암막 — 알파 포함")]
    public Color Scrim = new Color32(0x00, 0x00, 0x00, 0xE6);


    [Tooltip("경고/위험 강조색")]
    public Color Danger = new Color32(0xEA, 0x00, 0x00, 0xFF);

    [Header("Text Ink")]
    [Tooltip("본문/제목 기본 잉크 — 어두운 배경 위 밝은 텍스트")]
    public Color TextPrimary = new Color32(0xF2, 0xF8, 0xFF, 0xFF);

    [Tooltip("보조 설명, 비활성 탭 라벨")]
    public Color TextSecondary = new Color32(0xA8, 0xB9, 0xCC, 0xFF);

    [Tooltip("플레이스홀더처럼 가장 약한 텍스트")]
    public Color TextMuted = new Color32(0x83, 0xA3, 0xAD, 0xFF);

    [Tooltip("Accent 배경 위에 올라가는 텍스트")]
    public Color TextOnAccent = new Color32(0x0A, 0x0F, 0x1D, 0xFF);

    [Tooltip("조건 충족/성공 — 순수 green은 어두운 배경에서 튀므로 낮춘 톤")]
    public Color TextPositive = new Color32(0x33, 0xE5, 0x66, 0xFF);

    [Tooltip("조건 미충족/실패 — 순수 red보다 가독성이 좋은 톤")]
    public Color TextNegative = new Color32(0xFF, 0x6B, 0x72, 0xFF);

    [Tooltip("주의/잠김 안내")]
    public Color TextWarning = new Color32(0xFF, 0xB3, 0x47, 0xFF);

    [Header("Gauge Stages")]
    [Tooltip("게이지 10% 미만")]
    public Color GaugeCritical = new Color32(0xFF, 0x3B, 0x3B, 0xFF);

    [Tooltip("게이지 40% 미만")]
    public Color GaugeLow = new Color32(0xFF, 0x8C, 0x00, 0xFF);

    [Tooltip("게이지 70% 미만")]
    public Color GaugeMedium = new Color32(0xFF, 0xE0, 0x1A, 0xFF);

    [Tooltip("게이지 70% 이상")]
    public Color GaugeHigh = new Color32(0x33, 0xE5, 0x66, 0xFF);

    [Header("Graphic Roles")]
    [Tooltip("아이템 슬롯 배경 — 팔레트 밖의 녹색 기운을 뺀 중성 톤")]
    public Color SlotSurface = new Color32(0xC2, 0xCB, 0xD4, 0xB1);

    [Tooltip("진행도/게이지 채움 기본색")]
    public Color ProgressFill = new Color32(0x00, 0xE5, 0xFF, 0xFF);

    [Tooltip("체력 게이지 색")]
    public Color HealthFill = new Color32(0xEA, 0x00, 0x00, 0xFF);

    [Tooltip("스태미나 게이지 색")]
    public Color StaminaFill = new Color32(0x3C, 0x78, 0xFF, 0xFF);

    [Tooltip("모달 패널 뒤를 덮는 딤머 색. 블러 배경 위에 얹히므로 알파로 어둡기를 조절한다.")]
    public Color Dimmer = new Color32(0x00, 0x00, 0x00, 0x99);

    [Header("Backdrop Blur")]
    [Tooltip("패널 뒤 실시간 블러 배경에 쓰는 머티리얼 (M_UIBlurBackdrop). UIRealtimeBackdropBlur가 비어 있으면 여기서 가져간다.")]
    public Material BackdropBlurMaterial;

    [Tooltip("블러에 곱할 색. RGB를 낮추면 어두워지고, 알파는 배경 전체의 불투명도.")]
    public Color BackdropBlurTint = new Color(0.35f, 0.38f, 0.45f, 1f);

    [Header("Typography (px)")]
    public float FontSizeCaption = 15f;
    public float FontSizeBody = 18f;
    public float FontSizeSubtitle = 24f;
    public float FontSizeTitle = 30f;
    public float FontSizeDisplay = 36f;

    [Tooltip("전체화면 오버레이 문구처럼 Display보다 커야 하는 텍스트")]
    public float FontSizeHero = 60f;

    [Tooltip("타이틀 로고 등 단독으로 쓰이는 최대 크기")]
    public float FontSizeBanner = 120f;

    [Header("Typography Auto Size Minimum (px)")]
    public float FontSizeCaptionMin = 12f;
    public float FontSizeBodyMin = 14f;
    public float FontSizeSubtitleMin = 16f;
    public float FontSizeTitleMin = 18f;
    public float FontSizeDisplayMin = 24f;
    public float FontSizeHeroMin = 36f;
    public float FontSizeBannerMin = 60f;

    [Header("Layout")]
    [Min(1), Tooltip("UI 간격과 패딩을 맞추는 최소 그리드 단위")]
    public int SpacingGrid = 4;

    [HideInInspector]
    public int DesignSystemVersion;

    [Header("Button Interaction")]
    [Tooltip("ColorTint는 스프라이트 색을 곱하므로, 기본값을 살짝 어둡게 두고 호버에서 흰색(100%)으로 올려 밝아지게 만든다.")]
    public Color ButtonNormal = new Color32(0xDC, 0xDC, 0xDC, 0xFF);
    public Color ButtonHighlighted = Color.white;
    public Color ButtonPressed = new Color32(0x9A, 0xE4, 0xEF, 0xFF);
    public Color ButtonSelected = Color.white;
    public Color ButtonDisabled = new Color32(0x64, 0x64, 0x64, 0x78);

    [Range(0f, 0.5f)] public float ButtonFadeDuration = 0.08f;

    [Header("Button Motion")]
    [Tooltip("호버 시 버튼 확대 배율")]
    [Range(1f, 1.15f)] public float ButtonHoverScale = 1.03f;

    [Tooltip("누를 때 축소 배율")]
    [Range(0.85f, 1f)] public float ButtonPressScale = 0.97f;

    [Range(0f, 0.3f)] public float ButtonMotionDuration = 0.08f;

    [Header("Slot Hover")]
    [Tooltip("슬롯에 마우스를 올렸을 때 테두리 강조색")]
    public Color SlotHoverBorder = new Color32(0x9A, 0xF4, 0xFF, 0xFF);

    [Range(1f, 1.15f)] public float SlotHoverScale = 1.04f;

    [Header("Button Style Overrides")]
    [Tooltip("Secondary 스타일의 기본 색 — 보조 버튼은 더 차분하게")]
    public Color SecondaryNormal = new Color32(0xB4, 0xB4, 0xB4, 0xFF);

    [Tooltip("Danger 스타일의 눌림 색")]
    public Color DangerPressed = new Color32(0xEA, 0x64, 0x64, 0xFF);

    // 코드에서 색을 직접 지정해야 할 때 쓰는 접근자.
    // 테마 에셋을 못 찾는 상황의 폴백 값을 이 클래스 한 곳에서만 관리한다.
    public static Color InkPrimary { get { return Default != null ? Default.TextPrimary : (Color)new Color32(0xF2, 0xF8, 0xFF, 0xFF); } }
    public static Color InkSecondary { get { return Default != null ? Default.TextSecondary : (Color)new Color32(0xA8, 0xB9, 0xCC, 0xFF); } }
    public static Color InkMuted { get { return Default != null ? Default.TextMuted : (Color)new Color32(0x83, 0xA3, 0xAD, 0xFF); } }
    public static Color InkPositive { get { return Default != null ? Default.TextPositive : (Color)new Color32(0x33, 0xE5, 0x66, 0xFF); } }
    public static Color InkNegative { get { return Default != null ? Default.TextNegative : (Color)new Color32(0xFF, 0x6B, 0x72, 0xFF); } }
    public static Color InkWarning { get { return Default != null ? Default.TextWarning : (Color)new Color32(0xFF, 0xB3, 0x47, 0xFF); } }
    public static Color AccentColor { get { return Default != null ? Default.Accent : (Color)new Color32(0x00, 0xE5, 0xFF, 0xFF); } }

    // 게이지 단계 임계값. CrankGaugeUI와 GeneratorUI가 같은 규칙을 각자 복사해 두고 있었다.
    public Color GetGaugeColor(float ratio01)
    {
        if (ratio01 < 0.1f) return GaugeCritical;
        if (ratio01 < 0.4f) return GaugeLow;
        if (ratio01 < 0.7f) return GaugeMedium;
        return GaugeHigh;
    }

    public static Color GaugeColorFor(float ratio01)
    {
        UITheme theme = Default;
        if (theme != null) return theme.GetGaugeColor(ratio01);

        if (ratio01 < 0.1f) return new Color32(0xFF, 0x3B, 0x3B, 0xFF);
        if (ratio01 < 0.4f) return new Color32(0xFF, 0x8C, 0x00, 0xFF);
        if (ratio01 < 0.7f) return new Color32(0xFF, 0xE0, 0x1A, 0xFF);
        return new Color32(0x33, 0xE5, 0x66, 0xFF);
    }

    public float GetFontSize(TypographyRole role)
    {
        return role switch
        {
            TypographyRole.Caption => FontSizeCaption,
            TypographyRole.Body => FontSizeBody,
            TypographyRole.Subtitle => FontSizeSubtitle,
            TypographyRole.Title => FontSizeTitle,
            TypographyRole.Display => FontSizeDisplay,
            TypographyRole.Hero => FontSizeHero,
            _ => FontSizeBanner,
        };
    }

    // TextColorRole.None은 호출자가 색을 적용하지 않아야 함을 뜻하므로 여기서는 다루지 않는다.
    public Color GetTextColor(TextColorRole role)
    {
        return role switch
        {
            TextColorRole.Secondary => TextSecondary,
            TextColorRole.Muted => TextMuted,
            TextColorRole.Accent => Accent,
            TextColorRole.OnAccent => TextOnAccent,
            TextColorRole.Positive => TextPositive,
            TextColorRole.Negative => TextNegative,
            TextColorRole.Warning => TextWarning,
            _ => TextPrimary,
        };
    }

    public Vector2 GetAutoSizeRange(TypographyRole role)
    {
        float minimum = role switch
        {
            TypographyRole.Caption => FontSizeCaptionMin,
            TypographyRole.Body => FontSizeBodyMin,
            TypographyRole.Subtitle => FontSizeSubtitleMin,
            TypographyRole.Title => FontSizeTitleMin,
            TypographyRole.Display => FontSizeDisplayMin,
            TypographyRole.Hero => FontSizeHeroMin,
            _ => FontSizeBannerMin,
        };
        return new Vector2(minimum, GetFontSize(role));
    }

    public ColorBlock GetSelectableColorBlock()
    {
        ColorBlock block = GetColorBlock(ButtonStyle.Primary);
        block.selectedColor = SlotHoverBorder;
        return block;
    }

    public ColorBlock GetColorBlock(ButtonStyle style)
    {
        var block = ColorBlock.defaultColorBlock;
        block.colorMultiplier = 1f;
        block.fadeDuration = ButtonFadeDuration;
        block.normalColor = ButtonNormal;
        block.highlightedColor = ButtonHighlighted;
        block.pressedColor = ButtonPressed;
        block.selectedColor = ButtonSelected;
        block.disabledColor = ButtonDisabled;

        switch (style)
        {
            case ButtonStyle.Secondary:
                block.normalColor = SecondaryNormal;
                break;
            case ButtonStyle.Danger:
                block.pressedColor = DangerPressed;
                break;
        }

        return block;
    }
}
