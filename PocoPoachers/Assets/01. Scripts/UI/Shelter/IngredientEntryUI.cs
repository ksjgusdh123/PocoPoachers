using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 재료 1종을 표시하는 엔트리 — 필요한 재료 수만큼 스폰해서 쓴다.
public class IngredientEntryUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _countText;

    public void Setup(ItemData item, int owned, int required)
    {
        if (item == null) return;

        if (_icon != null)
            _icon.sprite = ResourceManager.Instance.LoadSprite(item.icon);
        if (_nameText != null)
            _nameText.text = LocalizationManager.GetInstance().GetString(item.name);

        if (_countText == null) return;
        _countText.text = $"{owned} / {required}";
        _countText.color = owned >= required ? UITheme.InkPositive : UITheme.InkNegative;
    }
}
