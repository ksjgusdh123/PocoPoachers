using System.Linq;
using System.Text;
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
    private Inventory _inventory;

    private void Awake()
    {
        _playerStat = GetComponent<PlayerStat>();
        _inventory = GetComponent<Inventory>();
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
        if (IsMaxLevel(statType)) return "MAX";

        var costData = GetCostData(statType);
        if (costData == null) return $"Parts x{GetCostAmount(statType)}";

        var sb = new StringBuilder();
        AppendCostText(sb, costData.NeedItem1Id, costData.NeedItem1Count);
        AppendCostText(sb, costData.NeedItem2Id, costData.NeedItem2Count);

        return sb.Length > 0 ? sb.ToString() : $"Parts x{GetCostAmount(statType)}";
    }

    private void AppendCostText(StringBuilder sb, int itemId, int amount)
    {
        if (itemId <= 0 || amount <= 0) return;

        var itemData = DataManager.GetItem(itemId);
        string itemName = itemData != null ? LocalizationManager.GetInstance().GetString(itemData.ItemName) : $"Unknown Item({itemId})";

        if (sb.Length > 0) sb.Append(", ");
        sb.Append(itemName);
        sb.Append(" x");
        sb.Append(amount);
    }

    public bool TryEnhance(EnhancementStatType statType)
    {
        if (IsMaxLevel(statType)) return false;
        if (!CanConsumeCost(statType)) return false;

        ConsumeCost(statType);
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

    private EnhancementCostData GetCostData(EnhancementStatType statType)
    {
        int nextLevel = GetLevel(statType) + 1;
        return EnhancementCostTable.Instance.All.FirstOrDefault(d => d.stat == statType.ToString() && d.level == nextLevel);
    }

    private bool CanConsumeCost(EnhancementStatType statType)
    {
        var costData = GetCostData(statType);
        if (costData == null) return true;
        if (_inventory == null) return false;

        return HasItem(costData.NeedItem1Id, costData.NeedItem1Count)
            && HasItem(costData.NeedItem2Id, costData.NeedItem2Count);
    }

    private bool HasItem(int itemId, int amount)
    {
        if (itemId <= 0 || amount <= 0) return true;

        var itemData = DataManager.GetItem(itemId);
        return itemData != null && _inventory.HasItem(itemData, amount);
    }

    private void ConsumeCost(EnhancementStatType statType)
    {
        var costData = GetCostData(statType);
        if (costData == null) return;

        RemoveItem(costData.NeedItem1Id, costData.NeedItem1Count);
        RemoveItem(costData.NeedItem2Id, costData.NeedItem2Count);
    }

    private void RemoveItem(int itemId, int amount)
    {
        if (itemId <= 0 || amount <= 0) return;

        var itemData = DataManager.GetItem(itemId);
        if (itemData == null) return;

        _inventory.RemoveItem(itemData, amount);
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
