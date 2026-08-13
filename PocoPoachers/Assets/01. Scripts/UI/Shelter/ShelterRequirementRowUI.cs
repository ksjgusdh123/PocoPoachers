using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 쉘터 업그레이드에 필요한 재료 한 줄. ShelterUpgradeUI가 필요한 개수만큼 찍어낸다.
public class ShelterRequirementRowUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _countText;

    public void Set(int itemId, int current, int required)
    {
        var itemData = ItemTable.Instance.Get(itemId);

        // 인벤토리 슬롯(SlotUIBase.SetIcon)과 같은 경로로 아이콘을 불러온다
        if (_icon != null)
        {
            _icon.sprite = itemData != null ? ResourceManager.Instance.LoadSprite(itemData.icon) : null;
            _icon.gameObject.SetActive(_icon.sprite != null);
        }

        if (_nameText != null)
            _nameText.text = itemData != null
                ? LocalizationManager.GetInstance().GetString(itemData.Name)
                : $"ID:{itemId}";

        if (_countText == null) return;

        _countText.text = $"{current} / {required}";
        _countText.color = current >= required ? UITheme.AccentColor : UITheme.InkNegative;
    }
}
