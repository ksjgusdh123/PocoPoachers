using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShelterUpgradeUI : UIBase
{
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private GameObject _requirementsPanel;
    [SerializeField] private TextMeshProUGUI _item1NameText;
    [SerializeField] private TextMeshProUGUI _item1CountText;
    [SerializeField] private GameObject _item2Row;
    [SerializeField] private TextMeshProUGUI _item2NameText;
    [SerializeField] private TextMeshProUGUI _item2CountText;
    [SerializeField] private Button _upgradeButton;
    [SerializeField] private TextMeshProUGUI _maxLevelText;

    protected override UIType UiType => UIType.ShelterUpgrade;

    private Inventory _storage;

    public void Open(Inventory storage)
    {
        _storage = storage;
        Refresh();
    }

    private void Refresh()
    {
        var shelter = ShelterManager.GetInstance();
        _levelText.text = $"쉘터 Lv. {shelter.CurrentLevel}";

        var next = shelter.GetNextLevelData();
        if (next == null)
        {
            _requirementsPanel.SetActive(false);
            _upgradeButton.gameObject.SetActive(false);
            _maxLevelText.gameObject.SetActive(true);
            return;
        }

        _requirementsPanel.SetActive(true);
        _upgradeButton.gameObject.SetActive(true);
        _maxLevelText.gameObject.SetActive(false);

        SetItemRow(_item1NameText, _item1CountText, next.NeedItem1Id, next.NeedItem1Count);

        bool hasItem2 = next.NeedItem2Id != 0;
        _item2Row.SetActive(hasItem2);
        if (hasItem2)
            SetItemRow(_item2NameText, _item2CountText, next.NeedItem2Id, next.NeedItem2Count);

        _upgradeButton.interactable = shelter.HasRequiredItems(_storage, next);
    }

    private void SetItemRow(TextMeshProUGUI nameText, TextMeshProUGUI countText, int itemId, int required)
    {
        var itemData = ItemTable.Instance.Get(itemId);
        nameText.text = itemData != null ? LocalizationManager.GetInstance().GetString(itemData.Name) : $"ID:{itemId}";
        int current = (itemData != null && _storage != null) ? _storage.GetItemCount(itemData) : 0;
        countText.text = $"{current} / {required}";
    }

    public void OnClickUpgrade()
    {
        if (!ShelterManager.GetInstance().TryUpgrade(_storage)) return;
        Refresh();
    }
}
