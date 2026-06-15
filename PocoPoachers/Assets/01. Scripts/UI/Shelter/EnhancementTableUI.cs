using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum EnhancementStatType
{
    MaxHp,
    MaxBattery,
    MaxStamina,
    MoveSpeed,
}

public class EnhancementTableUI : MonoBehaviour
{
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
    [SerializeField] private Button _enhanceButton;

    private PlayerController _player;
    private PlayerStat _playerStat;
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

    public void Open(PlayerController player)
    {
        _player = player;
        _playerStat = player != null ? player.GetComponent<PlayerStat>() : null;

        SelectStat(_selectedStatType);
    }

    public void Refresh()
    {
        if (_playerStat == null) return;

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
    }

    private void SelectStat(EnhancementStatType statType)
    {
        _selectedStatType = statType;
        Refresh();
    }

    private void OnClickEnhance()
    {
        EnhanceRequested?.Invoke(_selectedStatType);
        Debug.Log($"Enhancement requested: {_selectedStatType}");
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

    private static float GetPreviewIncrease(EnhancementStatType statType)
    {
        return statType switch
        {
            EnhancementStatType.MaxHp => 10f,
            EnhancementStatType.MaxBattery => 10f,
            EnhancementStatType.MaxStamina => 10f,
            EnhancementStatType.MoveSpeed => 0.25f,
            _ => 0f
        };
    }

    private static int GetCurrentLevel(EnhancementStatType statType)
    {
        return 0;
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

    private static string GetCostText(EnhancementStatType statType)
    {
        return "Cost not set";
    }

    private static string FormatValue(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value))
            ? Mathf.RoundToInt(value).ToString()
            : value.ToString("0.##");
    }
}
