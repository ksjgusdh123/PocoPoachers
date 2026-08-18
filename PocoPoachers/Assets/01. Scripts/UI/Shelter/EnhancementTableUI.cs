using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhancementTableUI : MonoBehaviour
{
    private const float LevelUpPowerCost = 50f;

    [Header("기체 레벨업")]
    [SerializeField] private TextMeshProUGUI _characterLevelText;
    [SerializeField] private TextMeshProUGUI _levelUpCostText;
    [SerializeField] private TextMeshProUGUI _levelUpPowerCostText;
    [SerializeField] private Button _levelUpButton;

    [Header("Stat Select Buttons")]
    [SerializeField] private Button _attackPowerButton;
    [SerializeField] private Button _moveSpeedButton;
    [SerializeField] private Button _maxHpButton;
    [SerializeField] private Button _defenseRateButton;
    [SerializeField] private Button _visionRangeButton;
    [SerializeField] private Button _attackSpeedButton;

    [Header("Detail Panel")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _valueText;
    [SerializeField] private TextMeshProUGUI _statPointsText;
    [SerializeField] private Button _enhanceButton;

    private PlayerController _player;
    private PlayerEnhancement _playerEnhancement;
    private EnhancementStatType _selectedStatType = EnhancementStatType.MaxHp;

    public event Action<EnhancementStatType> EnhanceRequested;

    private Button[] StatButtons => new[] { _attackPowerButton, _moveSpeedButton, _maxHpButton, _defenseRateButton, _visionRangeButton, _attackSpeedButton };
    private static readonly EnhancementStatType[] StatTypes =
    {
        EnhancementStatType.AttackPower,
        EnhancementStatType.MoveSpeed,
        EnhancementStatType.MaxHp,
        EnhancementStatType.DefenseRate,
        EnhancementStatType.VisionRange,
        EnhancementStatType.AttackSpeed,
    };

    private void Awake()
    {
        var buttons = StatButtons;
        for (int i = 0; i < buttons.Length; i++)
        {
            var statType = StatTypes[i];
            buttons[i]?.onClick.AddListener(() => SelectStat(statType));
        }

        _enhanceButton?.onClick.AddListener(OnClickEnhance);
        _levelUpButton?.onClick.AddListener(OnClickLevelUp);
    }

    private void OnEnable()
    {
        if (Generator.Instance != null)
            Generator.Instance.OnPowerChanged += HandlePowerChanged;
    }

    private void OnDisable()
    {
        if (Generator.Instance != null)
            Generator.Instance.OnPowerChanged -= HandlePowerChanged;
    }

    private void HandlePowerChanged(float current, float max) => RefreshLevelUpPanel();

    public void Open(PlayerController player)
    {
        _player = player;
        _playerEnhancement = player != null ? player.GetComponent<PlayerEnhancement>() : null;

        if (_playerEnhancement == null)
            Debug.LogWarning("EnhancementTableUI requires PlayerEnhancement on player.");

        SelectStat(_selectedStatType);
    }

    public void Refresh()
    {
        RefreshLevelUpPanel();

        if (_playerEnhancement == null)
        {
            SetUnavailable();
            return;
        }

        int level = _playerEnhancement.GetStatLevel(_selectedStatType);
        float currentBonus = _playerEnhancement.GetBonusAtLevel(_selectedStatType, level);
        float nextBonus = _playerEnhancement.GetBonusAtLevel(_selectedStatType, level + 1);

        if (_nameText != null)
            _nameText.text = GetDisplayName(_selectedStatType);

        if (_levelText != null)
            _levelText.text = $"Lv. {level}";

        if (_valueText != null)
            _valueText.text = $"{FormatBonus(_selectedStatType, currentBonus)} > {FormatBonus(_selectedStatType, nextBonus)}";

        if (_statPointsText != null)
            _statPointsText.text = $"{LocalizationManager.GetInstance().GetString("enhancement.stat_points")}: {_playerEnhancement.StatPoints}";

        if (_enhanceButton != null)
            _enhanceButton.interactable = _playerEnhancement.StatPoints > 0;
    }

    private void RefreshLevelUpPanel()
    {
        if (_playerEnhancement == null)
        {
            if (_characterLevelText != null) _characterLevelText.text = "-";
            if (_levelUpCostText != null) _levelUpCostText.text = "-";
            if (_levelUpButton != null) _levelUpButton.interactable = false;
            return;
        }

        if (_characterLevelText != null)
            _characterLevelText.text = $"Lv. {_playerEnhancement.CharacterLevel} / {_playerEnhancement.MaxCharacterLevel}";

        if (_levelUpCostText != null)
            _levelUpCostText.text = _playerEnhancement.GetCharacterLevelCostText();

        bool canAffordPower = Generator.Instance != null && Generator.Instance.CurrentPower >= LevelUpPowerCost;

        if (_levelUpPowerCostText != null)
        {
            _levelUpPowerCostText.text = string.Format(LocalizationManager.GetInstance().GetString("generator.power_cost_format"), LevelUpPowerCost.ToString("0"));
            _levelUpPowerCostText.color = canAffordPower ? UITheme.InkPositive : UITheme.InkNegative;
        }

        if (_levelUpButton != null)
            _levelUpButton.interactable = !_playerEnhancement.IsCharacterMaxLevel() && canAffordPower;
    }

    private void SelectStat(EnhancementStatType statType)
    {
        _selectedStatType = statType;
        RefreshStatSelection();
        Refresh();
    }

    private void RefreshStatSelection()
    {
        Button[] buttons = StatButtons;

        Color selectedColor = UITheme.InkPrimary;
        Color normalColor = UITheme.InkSecondary;

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            if (button == null) continue;

            bool selected = StatTypes[i] == _selectedStatType;
            Transform accent = button.transform.Find("SelectionAccent");
            if (accent != null) accent.gameObject.SetActive(selected);

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.color = selected ? selectedColor : normalColor;
        }
    }

    private void OnClickEnhance()
    {
        EnhanceRequested?.Invoke(_selectedStatType);

        if (_playerEnhancement == null) return;
        if (!_playerEnhancement.TrySpendPoint(_selectedStatType)) return;

        Refresh();
    }

    private void OnClickLevelUp()
    {
        if (_playerEnhancement == null) return;

        if (Generator.Instance == null || Generator.Instance.CurrentPower < LevelUpPowerCost)
        {
            var loc = LocalizationManager.GetInstance();
            UIManager.GetInstance().ShowNotice(loc.GetString("generator.title"), loc.GetString("generator.power_insufficient_message"));
            return;
        }

        if (!_playerEnhancement.TryLevelUpCharacter()) return;

        Generator.Instance.TryConsume(LevelUpPowerCost);
        Refresh();
    }

    private static string GetDisplayName(EnhancementStatType statType)
    {
        return statType switch
        {
            EnhancementStatType.AttackPower => "공격력",
            EnhancementStatType.MoveSpeed => "이동속도",
            EnhancementStatType.MaxHp => "체력",
            EnhancementStatType.DefenseRate => "방어력",
            EnhancementStatType.VisionRange => "시야",
            EnhancementStatType.AttackSpeed => "공격속도",
            _ => statType.ToString()
        };
    }

    // 배율형(공격력/공격속도)은 %로, 가산형은 소숫점 값 그대로 표시
    private static string FormatBonus(EnhancementStatType statType, float bonus)
    {
        if (statType is EnhancementStatType.AttackPower or EnhancementStatType.AttackSpeed)
        {
            int pct = Mathf.RoundToInt((bonus - 1f) * 100f);
            return pct >= 0 ? $"+{pct}%" : $"{pct}%";
        }

        if (statType == EnhancementStatType.DefenseRate)
            return $"+{bonus * 100f:F0}%";

        return Mathf.Approximately(bonus, Mathf.Round(bonus))
            ? $"+{Mathf.RoundToInt(bonus)}"
            : $"+{bonus:0.##}";
    }

    private void SetUnavailable()
    {
        if (_nameText != null)
            _nameText.text = "Unavailable";

        if (_levelText != null)
            _levelText.text = "Lv. -";

        if (_valueText != null)
            _valueText.text = "-";

        if (_statPointsText != null)
            _statPointsText.text = "Missing PlayerEnhancement";

        if (_enhanceButton != null)
            _enhanceButton.interactable = false;
    }
}
