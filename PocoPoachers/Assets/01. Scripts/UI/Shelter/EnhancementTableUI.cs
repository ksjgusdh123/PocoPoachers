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
    [Serializable]
    private class StatRow
    {
        [SerializeField] private EnhancementStatType _statType;
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _valueText;
        [SerializeField] private TextMeshProUGUI _costText;
        [SerializeField] private Button _enhanceButton;

        public EnhancementStatType StatType => _statType;
        public Button EnhanceButton => _enhanceButton;

        public void Refresh(string statName, int level, float currentValue, float nextValue, string costText)
        {
            if (_nameText != null)
                _nameText.text = statName;

            if (_levelText != null)
                _levelText.text = $"Lv. {level}";

            if (_valueText != null)
                _valueText.text = $"{FormatValue(currentValue)} > {FormatValue(nextValue)}";

            if (_costText != null)
                _costText.text = costText;
        }

        private static string FormatValue(float value)
        {
            return Mathf.Approximately(value, Mathf.Round(value))
                ? Mathf.RoundToInt(value).ToString()
                : value.ToString("0.##");
        }
    }

    [SerializeField] private StatRow[] _rows;

    private PlayerController _player;
    private PlayerStat _playerStat;

    public event Action<EnhancementStatType> EnhanceRequested;

    private void Awake()
    {
        if (_rows == null) return;

        foreach (var row in _rows)
        {
            if (row?.EnhanceButton == null) continue;

            EnhancementStatType statType = row.StatType;
            row.EnhanceButton.onClick.AddListener(() => OnClickEnhance(statType));
        }
    }

    public void Open(PlayerController player)
    {
        _player = player;
        _playerStat = player != null ? player.GetComponent<PlayerStat>() : null;

        Refresh();
    }

    public void Refresh()
    {
        if (_playerStat == null || _rows == null) return;

        foreach (var row in _rows)
        {
            if (row == null) continue;

            EnhancementStatType statType = row.StatType;
            float currentValue = GetCurrentValue(statType);
            float nextValue = currentValue + GetPreviewIncrease(statType);

            row.Refresh(
                GetDisplayName(statType),
                GetCurrentLevel(statType),
                currentValue,
                nextValue,
                GetCostText(statType));
        }
    }

    private void OnClickEnhance(EnhancementStatType statType)
    {
        EnhanceRequested?.Invoke(statType);
        Debug.Log($"Enhancement requested: {statType}");
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
}
