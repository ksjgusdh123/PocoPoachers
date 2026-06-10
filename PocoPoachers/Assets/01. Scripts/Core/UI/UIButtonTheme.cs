using TMPro;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "UIButtonTheme", menuName = "UI/Button Theme")]
public class UIButtonTheme : ScriptableObject
{
    public Sprite buttonSprite;
    public ColorBlock buttonColors = ColorBlock.defaultColorBlock;
    public TMP_FontAsset buttonFont;
    public Color buttonTextColor = Color.white;
}
