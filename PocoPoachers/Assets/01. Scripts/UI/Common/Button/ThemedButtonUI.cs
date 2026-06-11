using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ThemedButtonUI : MonoBehaviour
{
    [SerializeField] private UIButtonTheme _theme;

    private void Awake()
    {
        var button = GetComponent<Button>();

        if (_theme != null)
            UIThemeUtil.ApplyButtonStyle(button, _theme.buttonSprite, _theme.buttonColors, _theme.buttonFont, _theme.buttonTextColor);
    }
}
