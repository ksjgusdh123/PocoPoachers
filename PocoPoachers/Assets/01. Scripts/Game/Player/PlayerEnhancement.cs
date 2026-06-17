using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

[RequireComponent(typeof(PlayerStat))]
public class PlayerEnhancement : MonoBehaviour
{
    [Serializable]
    private class EnhancementMaterialCost
    {
        public int itemId = 0;
        public int amount = 1;
    }

    [Serializable]
    private class EnhancementLevelCost
    {
        public List<EnhancementMaterialCost> materials = new();
    }

    [Serializable]
    private class EnhancementCostTable
    {
        public EnhancementStatType statType = EnhancementStatType.MaxHp;
        public List<EnhancementLevelCost> levelCosts = new();
    }

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
    [SerializeField] private List<EnhancementCostTable> _costTables = new();

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

        var levelCost = GetLevelCost(statType);
        if (levelCost == null || levelCost.materials == null || levelCost.materials.Count == 0)
            return $"Parts x{GetCostAmount(statType)}";

        var sb = new StringBuilder();
        foreach (var materialCost in levelCost.materials)
        {
            if (materialCost == null || materialCost.amount <= 0) continue;

            var itemData = DataManager.GetItem(materialCost.itemId);
            string itemName = itemData != null ? LocalizationManager.GetInstance().GetString(itemData.ItemName) : $"Unknown Item({materialCost.itemId})";

            if (sb.Length > 0) sb.Append(", ");
            sb.Append(itemName);
            sb.Append(" x");
            sb.Append(materialCost.amount);
        }

        return sb.Length > 0 ? sb.ToString() : $"Parts x{GetCostAmount(statType)}";
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

    private EnhancementLevelCost GetLevelCost(EnhancementStatType statType)
    {
        int level = GetLevel(statType);

        foreach (var costTable in _costTables)
        {
            if (costTable == null || costTable.statType != statType) continue;
            if (costTable.levelCosts == null || level < 0 || level >= costTable.levelCosts.Count) return null;

            return costTable.levelCosts[level];
        }

        return null;
    }

    private bool CanConsumeCost(EnhancementStatType statType)
    {
        var levelCost = GetLevelCost(statType);
        if (levelCost == null || levelCost.materials == null || levelCost.materials.Count == 0)
            return true;

        if (_inventory == null) return false;

        foreach (var pair in GetRequiredAmounts(levelCost))
        {
            var itemData = DataManager.GetItem(pair.Key);
            if (itemData == null) return false;
            if (!_inventory.HasItem(itemData, pair.Value)) return false;
        }

        return true;
    }

    private void ConsumeCost(EnhancementStatType statType)
    {
        var levelCost = GetLevelCost(statType);
        if (levelCost == null || levelCost.materials == null || levelCost.materials.Count == 0)
            return;

        foreach (var pair in GetRequiredAmounts(levelCost))
        {
            var itemData = DataManager.GetItem(pair.Key);
            if (itemData == null) continue;

            _inventory.RemoveItem(itemData, pair.Value);
        }
    }

    private Dictionary<int, int> GetRequiredAmounts(EnhancementLevelCost levelCost)
    {
        var requiredAmounts = new Dictionary<int, int>();

        foreach (var materialCost in levelCost.materials)
        {
            if (materialCost == null || materialCost.itemId <= 0 || materialCost.amount <= 0) continue;

            if (requiredAmounts.ContainsKey(materialCost.itemId))
                requiredAmounts[materialCost.itemId] += materialCost.amount;
            else
                requiredAmounts[materialCost.itemId] = materialCost.amount;
        }

        return requiredAmounts;
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
