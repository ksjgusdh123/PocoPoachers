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
    private Inventory _player;

    protected override void Awake()
    {
        base.Awake();
        _upgradeButton.onClick.AddListener(OnClickUpgrade);
    }

    protected override void OnDestroy()
    {
        if (_upgradeButton != null)
            _upgradeButton.onClick.RemoveListener(OnClickUpgrade);
        base.OnDestroy();
    }

    private void OnEnable()
    {
        Refresh();
        LocalizationManager.GetInstance().OnLanguageChanged += Refresh;
    }

    private void OnDisable()
    {
        var manager = LocalizationManager.ExistingInstance;
        if (manager == null) return;
        manager.OnLanguageChanged -= Refresh;
    }

    public void Open(Inventory storage, Inventory player)
    {
        _storage = storage;
        _player = player;
        Refresh();
    }

    private void Refresh()
    {
        var shelter = ShelterManager.GetInstance();
        _levelText.text = string.Format(LocalizationManager.GetInstance().GetString("shelter.level_format"), shelter.CurrentLevel);

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

        _upgradeButton.interactable = shelter.HasRequiredItems(_player, _storage, next);
    }

    private void SetItemRow(TextMeshProUGUI nameText, TextMeshProUGUI countText, int itemId, int required)
    {
        var itemData = ItemTable.Instance.Get(itemId);
        nameText.text = itemData != null ? LocalizationManager.GetInstance().GetString(itemData.Name) : $"ID:{itemId}";
        int current = ShelterManager.GetInstance().GetCombinedItemCount(_player, _storage, itemId);
        countText.text = $"{current} / {required}";

        countText.color = current >= required ? UITheme.AccentColor : UITheme.InkNegative;
    }

    public void OnClickUpgrade()
    {
        if (!ShelterManager.GetInstance().TryUpgrade(_player, _storage)) return;

        if (_storage != null)
            SaveManager.GetInstance().SaveInventory("storage", _storage);

        Refresh();
    }
}
