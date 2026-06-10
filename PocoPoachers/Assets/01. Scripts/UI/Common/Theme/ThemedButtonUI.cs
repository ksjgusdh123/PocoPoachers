using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ThemedButtonUI : MonoBehaviour
{
    [SerializeField] private UIButtonTheme _theme;

    private void Awake()
    {
        if (_theme == null) return;
        UIThemeUtil.ApplyButtonStyle(GetComponent<Button>(), _theme.buttonSprite, _theme.buttonColors, _theme.buttonFont, _theme.buttonTextColor);
    }
}
