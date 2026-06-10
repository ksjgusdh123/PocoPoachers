using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class UIThemeUtil
{
    public static void ApplyText(TextMeshProUGUI text, TMP_FontAsset font, Color color)
    {
        if (text == null) return;
        if (font != null) text.font = font;
        text.color = color;
    }

    public static void ApplyButtonStyle(Button button, Sprite sprite, ColorBlock colors, TMP_FontAsset font, Color textColor)
    {
        if (sprite != null && button.TryGetComponent<Image>(out var image))
            image.sprite = sprite;
        button.colors = colors;
        ApplyText(button.GetComponentInChildren<TextMeshProUGUI>(), font, textColor);
    }
}
