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

    [Header("Palette")]
    [Tooltip("강조색 — 게이지, 테두리, 활성 상태에 사용")]
    public Color Accent = new Color32(0x00, 0xE5, 0xFF, 0xFF);

    [Tooltip("패널 배경색")]
    public Color Surface = new Color32(0x0A, 0x0F, 0x1D, 0xFF);

    [Tooltip("경고/위험 강조색")]
    public Color Danger = new Color32(0xEA, 0x00, 0x00, 0xFF);

    [Header("Typography (px)")]
    public float FontSizeCaption = 15f;
    public float FontSizeBody = 18f;
    public float FontSizeSubtitle = 24f;
    public float FontSizeTitle = 30f;
    public float FontSizeDisplay = 36f;

    [Header("Button Interaction")]
    [Tooltip("ColorTint는 스프라이트 색을 곱하므로, 기본값을 살짝 어둡게 두고 호버에서 흰색(100%)으로 올려 밝아지게 만든다.")]
    public Color ButtonNormal = new Color32(0xDC, 0xDC, 0xDC, 0xFF);
    public Color ButtonHighlighted = Color.white;
    public Color ButtonPressed = new Color32(0x9A, 0xE4, 0xEF, 0xFF);
    public Color ButtonSelected = Color.white;
    public Color ButtonDisabled = new Color32(0x64, 0x64, 0x64, 0x78);

    [Range(0f, 0.5f)] public float ButtonFadeDuration = 0.08f;

    [Header("Button Style Overrides")]
    [Tooltip("Secondary 스타일의 기본 색 — 보조 버튼은 더 차분하게")]
    public Color SecondaryNormal = new Color32(0xB4, 0xB4, 0xB4, 0xFF);

    [Tooltip("Danger 스타일의 눌림 색")]
    public Color DangerPressed = new Color32(0xEA, 0x64, 0x64, 0xFF);

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
