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
    }

    [Header("Palette")]
    [Tooltip("강조색 — 게이지, 테두리, 활성 상태에 사용")]
    public Color Accent = new Color32(0x00, 0xE5, 0xFF, 0xFF);

    [Tooltip("패널 배경색")]
    public Color Surface = new Color32(0x0A, 0x0F, 0x1D, 0xFF);

    [Tooltip("경고/위험 강조색")]
    public Color Danger = new Color32(0xEA, 0x00, 0x00, 0xFF);

    [Header("Graphic Roles")]
    [Tooltip("아이템 슬롯 배경 — 팔레트 밖의 녹색 기운을 뺀 중성 톤")]
    public Color SlotSurface = new Color32(0xC2, 0xCB, 0xD4, 0xB1);

    [Tooltip("진행도/게이지 채움 기본색")]
    public Color ProgressFill = new Color32(0x00, 0xE5, 0xFF, 0xFF);

    [Tooltip("체력 게이지 색")]
    public Color HealthFill = new Color32(0xEA, 0x00, 0x00, 0xFF);

    [Tooltip("스태미나 게이지 색")]
    public Color StaminaFill = new Color32(0x3C, 0x78, 0xFF, 0xFF);

    [Tooltip("모달 패널 뒤를 덮는 딤머 색")]
    public Color Dimmer = new Color32(0x0A, 0x0F, 0x1D, 0x99);

    [Header("Typography (px)")]
    public float FontSizeCaption = 15f;
    public float FontSizeBody = 18f;
    public float FontSizeSubtitle = 24f;
    public float FontSizeTitle = 30f;
    public float FontSizeDisplay = 36f;

    [Header("Typography Auto Size Minimum (px)")]
    public float FontSizeCaptionMin = 12f;
    public float FontSizeBodyMin = 14f;
    public float FontSizeSubtitleMin = 16f;
    public float FontSizeTitleMin = 18f;
    public float FontSizeDisplayMin = 24f;

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

    public float GetFontSize(TypographyRole role)
    {
        return role switch
        {
            TypographyRole.Caption => FontSizeCaption,
            TypographyRole.Body => FontSizeBody,
            TypographyRole.Subtitle => FontSizeSubtitle,
            TypographyRole.Title => FontSizeTitle,
            _ => FontSizeDisplay,
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
            _ => FontSizeDisplayMin,
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
