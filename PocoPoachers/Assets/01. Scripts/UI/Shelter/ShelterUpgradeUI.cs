using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShelterUpgradeUI : UIBase
{
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private GameObject _requirementsPanel;

    [SerializeField, Tooltip("재료 한 줄 프리팹 — 다음 레벨에 필요한 재료 수만큼 이 아래에 찍어낸다")]
    private ShelterRequirementRowUI _rowPrefab;

    [SerializeField, Tooltip("재료 줄이 붙을 부모. 비워두면 RequirementsPanel 아래에 붙는다")]
    private Transform _rowParent;

    [SerializeField, Tooltip("업그레이드하면 무엇이 열리는지 한 줄 요약 — shelter.csv의 unlock_desc(로컬라이제이션 키)를 표시한다")]
    private TextMeshProUGUI _unlockDescText;

    [SerializeField] private Button _upgradeButton;
    [SerializeField] private TextMeshProUGUI _maxLevelText;

    protected override UIType UiType => UIType.ShelterUpgrade;

    private Inventory _storage;
    private Inventory _player;

    private readonly List<ShelterRequirementRowUI> _rows = new();

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
            ClearRows();
            _requirementsPanel.SetActive(false);
            _upgradeButton.gameObject.SetActive(false);
            _maxLevelText.gameObject.SetActive(true);
            if (_unlockDescText != null) _unlockDescText.gameObject.SetActive(false);
            return;
        }

        _requirementsPanel.SetActive(true);
        _upgradeButton.gameObject.SetActive(true);
        _maxLevelText.gameObject.SetActive(false);

        SetUnlockDesc(next);

        BuildRows(shelter, next);

        _upgradeButton.interactable = shelter.HasRequiredItems(_player, _storage, next);
    }

    // 업그레이드로 무엇이 열리는지 한 줄 요약. 키가 비어 있으면 줄 자체를 숨긴다.
    private void SetUnlockDesc(ShelterData next)
    {
        if (_unlockDescText == null) return;

        bool hasDesc = !string.IsNullOrEmpty(next.UnlockDesc);
        _unlockDescText.gameObject.SetActive(hasDesc);

        if (hasDesc)
            _unlockDescText.text = ToBulletList(LocalizationManager.GetInstance().GetString(next.UnlockDesc));
    }

    // 줄바꿈으로 나눠 적은 요약을 줄마다 "- "를 붙인 목록으로 만든다.
    // CSV에 \n으로 적으면 테이블 생성기가 진짜 줄바꿈으로 바꿔주지만,
    // 아직 생성기를 안 돌린 데이터도 있을 수 있어 문자 그대로의 \n도 함께 처리한다.
    private static string ToBulletList(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var builder = new StringBuilder();

        foreach (string line in text.Replace("\\n", "\n").Split('\n'))
        {
            string trimmed = line.Trim();
            if (trimmed.Length == 0) continue;

            if (builder.Length > 0) builder.Append('\n');
            builder.Append("- ").Append(trimmed);
        }

        return builder.ToString();
    }

    // 데이터가 가진 재료 목록을 그대로 줄로 찍어낸다.
    // 재료 개수가 늘어나면 GetRequirements만 고치면 된다.
    private void BuildRows(ShelterManager shelter, ShelterData next)
    {
        ClearRows();

        if (_rowPrefab == null)
        {
            Debug.LogWarning($"[{nameof(ShelterUpgradeUI)}] 재료 줄 프리팹이 비어 있습니다.", this);
            return;
        }

        Transform parent = _rowParent != null ? _rowParent : _requirementsPanel.transform;

        foreach ((int itemId, int required) in next.NeedItems)
        {
            var row = Instantiate(_rowPrefab, parent);
            row.Set(itemId, shelter.GetCombinedItemCount(_player, _storage, itemId), required);
            _rows.Add(row);
        }
    }

    private void ClearRows()
    {
        foreach (var row in _rows)
            if (row != null) Destroy(row.gameObject);

        _rows.Clear();
    }

    public void OnClickUpgrade()
    {
        if (!ShelterManager.GetInstance().TryUpgrade(_player, _storage)) return;

        if (_storage != null)
            SaveManager.GetInstance().SaveInventory("storage", _storage);

        Refresh();
    }
}
