using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnhancementTableUI : MonoBehaviour
{
    private const float PowerCost = 50f;

    [Header("Stat Select Buttons")]
    [SerializeField] private Button _hpButton;
    [SerializeField] private Button _batteryButton;
    [SerializeField] private Button _staminaButton;
    [SerializeField] private Button _speedButton;

    [Header("Detail Panel")]
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _valueText;
    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private TextMeshProUGUI _powerCostText;
    [SerializeField] private Button _enhanceButton;

    private PlayerController _player;
    private PlayerStat _playerStat;
    private PlayerEnhancement _playerEnhancement;
    private EnhancementStatType _selectedStatType = EnhancementStatType.MaxHp;

    public event Action<EnhancementStatType> EnhanceRequested;

    private void Awake()
    {
        _hpButton?.onClick.AddListener(() => SelectStat(EnhancementStatType.MaxHp));
        _batteryButton?.onClick.AddListener(() => SelectStat(EnhancementStatType.MaxBattery));
        _staminaButton?.onClick.AddListener(() => SelectStat(EnhancementStatType.MaxStamina));
        _speedButton?.onClick.AddListener(() => SelectStat(EnhancementStatType.MoveSpeed));
        _enhanceButton?.onClick.AddListener(OnClickEnhance);
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

    private void HandlePowerChanged(float current, float max) => RefreshPowerCostUI();

    public void Open(PlayerController player)
    {
        _player = player;
        _playerStat = player != null ? player.GetComponent<PlayerStat>() : null;
        _playerEnhancement = player != null ? player.GetComponent<PlayerEnhancement>() : null;

        if (_playerStat == null)
            Debug.LogWarning("EnhancementTableUI requires PlayerStat on player.");

        if (_playerEnhancement == null)
            Debug.LogWarning("EnhancementTableUI requires PlayerEnhancement on player.");

        SelectStat(_selectedStatType);
    }

    public void Refresh()
    {
        if (_playerStat == null || _playerEnhancement == null)
        {
            SetUnavailable();
            return;
        }

        float currentValue = GetCurrentValue(_selectedStatType);
        float nextValue = currentValue + GetPreviewIncrease(_selectedStatType);

        if (_nameText != null)
            _nameText.text = GetDisplayName(_selectedStatType);

        if (_levelText != null)
            _levelText.text = $"Lv. {GetCurrentLevel(_selectedStatType)}";

        if (_valueText != null)
            _valueText.text = $"{FormatValue(currentValue)} > {FormatValue(nextValue)}";

        if (_costText != null)
            _costText.text = GetCostText(_selectedStatType);

        RefreshPowerCostUI();
    }

    private void RefreshPowerCostUI()
    {
        bool canAffordPower = Generator.Instance != null && Generator.Instance.CurrentPower >= PowerCost;

        if (_powerCostText != null)
        {
            _powerCostText.text = $"전력 {PowerCost:0}";
            _powerCostText.color = canAffordPower ? Color.green : Color.red;
        }

        if (_enhanceButton != null)
            _enhanceButton.interactable = canAffordPower;
    }

    private void SelectStat(EnhancementStatType statType)
    {
        _selectedStatType = statType;
        Refresh();
    }

    private void OnClickEnhance()
    {
        EnhanceRequested?.Invoke(_selectedStatType);

        if (_playerEnhancement == null) return;

        if (Generator.Instance == null || Generator.Instance.CurrentPower < PowerCost)
        {
            UIManager.GetInstance().ShowNotice("발전기", "발전기 전력 부족");
            return;
        }

        if (!_playerEnhancement.TryEnhance(_selectedStatType)) return;

        Generator.Instance.TryConsume(PowerCost);
        Refresh();
    }

    private float GetCurrentValue(EnhancementStatType statType)
    {
        return statType switch
        {
            EnhancementStatType.MaxHp => _playerStat.MaxHp,
            EnhancementStatType.MaxBattery => _playerStat.MaxBattery,
            EnhancementStatType.MaxStamina => _playerStat.MaxStamina,
            EnhancementStatType.MoveSpeed => _playerStat.MoveSpeed,
            _ => 0f
        };
    }

    private float GetPreviewIncrease(EnhancementStatType statType)
    {
        return _playerEnhancement.GetNextIncrease(statType);
    }

    private int GetCurrentLevel(EnhancementStatType statType)
    {
        return _playerEnhancement.GetLevel(statType);
    }

    private static string GetDisplayName(EnhancementStatType statType)
    {
        return statType switch
        {
            EnhancementStatType.MaxHp => "HP",
            EnhancementStatType.MaxBattery => "Battery",
            EnhancementStatType.MaxStamina => "Stamina",
            EnhancementStatType.MoveSpeed => "Move Speed",
            _ => statType.ToString()
        };
    }

    private string GetCostText(EnhancementStatType statType)
    {
        return _playerEnhancement.GetCostText(statType);
    }

    private static string FormatValue(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value))
            ? Mathf.RoundToInt(value).ToString()
            : value.ToString("0.##");
    }

    private void SetUnavailable()
    {
        if (_nameText != null)
            _nameText.text = "Unavailable";

        if (_levelText != null)
            _levelText.text = "Lv. -";

        if (_valueText != null)
            _valueText.text = "-";

        if (_costText != null)
            _costText.text = "Missing PlayerEnhancement";

        if (_powerCostText != null)
            _powerCostText.text = "-";

        if (_enhanceButton != null)
            _enhanceButton.interactable = false;
    }
}
