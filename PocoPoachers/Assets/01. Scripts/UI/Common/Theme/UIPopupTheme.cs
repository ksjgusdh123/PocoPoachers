using TMPro;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "UIPopupTheme", menuName = "UI/Popup Theme")]
public class UIPopupTheme : ScriptableObject
{
    [Header("Panel")]
    public Sprite panelSprite;
    public Color panelColor = Color.white;

    [Header("Title")]
    public TMP_FontAsset titleFont;
    public Color titleColor = Color.white;

    [Header("Message")]
    public TMP_FontAsset messageFont;
    public Color messageColor = Color.white;

    [Header("Button")]
    public Sprite buttonSprite;
    public ColorBlock buttonColors = ColorBlock.defaultColorBlock;
    public TMP_FontAsset buttonFont;
    public Color buttonTextColor = Color.white;
}
