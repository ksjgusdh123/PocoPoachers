using UnityEngine;

[RequireComponent(typeof(PlayerStat))]
public class PlayerEnhancement : MonoBehaviour
{
    [Header("Level")]
    [SerializeField] private int _maxHpLevel;
    [SerializeField] private int _maxBatteryLevel;
    [SerializeField] private int _maxStaminaLevel;
    [SerializeField] private int _moveSpeedLevel;

    [Header("Config")]
    [SerializeField] private int _maxLevel = 10;
    [SerializeField] private int _baseCost = 2;
    [SerializeField] private int _costIncreasePerLevel = 2;
    [SerializeField] private float _maxHpIncreasePerLevel = 10f;
    [SerializeField] private float _maxBatteryIncreasePerLevel = 10f;
    [SerializeField] private float _maxStaminaIncreasePerLevel = 10f;
    [SerializeField] private float _moveSpeedIncreasePerLevel = 0.25f;

    private PlayerStat _playerStat;

    private void Awake()
    {
        _playerStat = GetComponent<PlayerStat>();
    }

    private void Start()
    {
        ApplyToPlayerStat();
    }

    public int GetLevel(EnhancementStatType statType)
    {
        return statType switch
        {
            EnhancementStatType.MaxHp => _maxHpLevel,
            EnhancementStatType.MaxBattery => _maxBatteryLevel,
            EnhancementStatType.MaxStamina => _maxStaminaLevel,
            EnhancementStatType.MoveSpeed => _moveSpeedLevel,
            _ => 0
        };
    }

    public bool IsMaxLevel(EnhancementStatType statType)
    {
        return GetLevel(statType) >= _maxLevel;
    }

    public float GetCurrentBonus(EnhancementStatType statType)
    {
        return GetLevel(statType) * GetIncreasePerLevel(statType);
    }

    public float GetNextIncrease(EnhancementStatType statType)
    {
        return IsMaxLevel(statType) ? 0f : GetIncreasePerLevel(statType);
    }

    public int GetCostAmount(EnhancementStatType statType)
    {
        if (IsMaxLevel(statType)) return 0;
        return _baseCost + GetLevel(statType) * _costIncreasePerLevel;
    }

    public string GetCostText(EnhancementStatType statType)
    {
        return IsMaxLevel(statType) ? "MAX" : $"Parts x{GetCostAmount(statType)}";
    }

    public bool TryEnhance(EnhancementStatType statType)
    {
        if (IsMaxLevel(statType)) return false;

        SetLevel(statType, GetLevel(statType) + 1);
        ApplyToPlayerStat();
        return true;
    }

    private void SetLevel(EnhancementStatType statType, int level)
    {
        level = Mathf.Clamp(level, 0, _maxLevel);

        switch (statType)
        {
            case EnhancementStatType.MaxHp:
                _maxHpLevel = level;
                break;
            case EnhancementStatType.MaxBattery:
                _maxBatteryLevel = level;
                break;
            case EnhancementStatType.MaxStamina:
                _maxStaminaLevel = level;
                break;
            case EnhancementStatType.MoveSpeed:
                _moveSpeedLevel = level;
                break;
        }
    }

    private float GetIncreasePerLevel(EnhancementStatType statType)
    {
        return statType switch
        {
            EnhancementStatType.MaxHp => _maxHpIncreasePerLevel,
            EnhancementStatType.MaxBattery => _maxBatteryIncreasePerLevel,
            EnhancementStatType.MaxStamina => _maxStaminaIncreasePerLevel,
            EnhancementStatType.MoveSpeed => _moveSpeedIncreasePerLevel,
            _ => 0f
        };
    }

    private void ApplyToPlayerStat()
    {
        if (_playerStat == null) return;

        _playerStat.ApplyEnhancementStats(
            GetCurrentBonus(EnhancementStatType.MaxHp),
            GetCurrentBonus(EnhancementStatType.MaxBattery),
            GetCurrentBonus(EnhancementStatType.MaxStamina),
            GetCurrentBonus(EnhancementStatType.MoveSpeed));
    }
}
