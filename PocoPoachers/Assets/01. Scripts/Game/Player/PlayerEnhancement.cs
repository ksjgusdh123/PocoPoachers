using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// 스탯별 레벨당 성장치. 인스펙터에 항목을 추가/삭제하는 것만으로 강화 가능한 스탯을 늘리거나 줄일 수 있다.
[Serializable]
public class StatGrowthConfig
{
    public EnhancementStatType statType;
    public float valuePerLevel;
    // true면 valuePerLevel을 배율 성장률로 해석한다: 최종 배율 = 1 + level * valuePerLevel (공격력/공격속도용)
    // false면 valuePerLevel만큼 매 레벨 가산한다 (체력/이동속도/방어력/시야용)
    public bool isPercentGrowth;
    // 강화 UI에 보여줄 강화 수치를 %로 표시할지. isPercentGrowth(성장 공식)와는 별개 —
    // 방어력처럼 가산형이어도 결과값 자체는 %로 보여주고 싶은 경우가 있다.
    public bool displayAsPercent;
}

[RequireComponent(typeof(PlayerStat))]
public class PlayerEnhancement : MonoBehaviour
{
    // 스탯 하나당 찍을 수 있는 최대 레벨 — 강화 UI의 10칸 블록바와 맞춘 값.
    public const int MaxStatLevel = 10;

    [Header("기체 레벨")]
    [SerializeField] private int _characterLevel;
    [SerializeField] private int _statPoints;
    [SerializeField] private int _maxCharacterLevel = 20;

    [Header("스탯별 성장률 (여기에 항목을 추가/삭제하면 강화 가능한 스탯이 바뀐다)")]
    [SerializeField] private List<StatGrowthConfig> _growthConfigs = new()
    {
        new StatGrowthConfig { statType = EnhancementStatType.AttackPower, valuePerLevel = 0.05f, isPercentGrowth = true,  displayAsPercent = true },
        new StatGrowthConfig { statType = EnhancementStatType.MoveSpeed,   valuePerLevel = 0.25f, isPercentGrowth = false, displayAsPercent = false },
        new StatGrowthConfig { statType = EnhancementStatType.MaxHp,       valuePerLevel = 10f,   isPercentGrowth = false, displayAsPercent = false },
        new StatGrowthConfig { statType = EnhancementStatType.DefenseRate, valuePerLevel = 0.02f, isPercentGrowth = false, displayAsPercent = true },
        new StatGrowthConfig { statType = EnhancementStatType.VisionRange, valuePerLevel = 1f,    isPercentGrowth = false, displayAsPercent = false },
        new StatGrowthConfig { statType = EnhancementStatType.AttackSpeed, valuePerLevel = 0.05f, isPercentGrowth = true,  displayAsPercent = true },
    };

    // 스탯별 현재 레벨 — 딕셔너리라 _growthConfigs에 없는 스탯이 들어와도, 새 스탯이 추가돼도 코드 변경 없이 동작한다.
    private readonly Dictionary<EnhancementStatType, int> _statLevels = new();

    private PlayerStat _playerStat;
    private PlayerVision _playerVision;
    private WeaponMount _weaponMount;
    private Inventory _inventory;

    public int CharacterLevel => _characterLevel;
    public int StatPoints => _statPoints;
    // 인스펙터의 _growthConfigs 순서 그대로 노출 — 강화 UI가 이 순서대로 행을 만들면 스탯 추가/삭제가 코드 변경 없이 반영된다.
    public IEnumerable<EnhancementStatType> ConfiguredStatTypes => _growthConfigs.Select(c => c.statType);
    public int MaxCharacterLevel => _maxCharacterLevel;

    public event Action OnChanged;

    private void Awake()
    {
        _playerStat = GetComponent<PlayerStat>();
        _playerVision = GetComponent<PlayerVision>();
        _weaponMount = GetComponent<WeaponMount>();
        _inventory = GetComponent<Inventory>();

        if (SaveManager.Instance != null &&
            SaveManager.Instance.TryLoadCharacterEnhancement(out int level, out int points, out var statLevels))
        {
            _characterLevel = level;
            _statPoints = points;
            foreach (var entry in statLevels)
                _statLevels[entry.statType] = entry.level;
        }
    }

    private void Start()
    {
        ApplyAll();
    }

    public int GetStatLevel(EnhancementStatType statType) =>
        _statLevels.TryGetValue(statType, out int level) ? level : 0;

    public bool IsCharacterMaxLevel() => _characterLevel >= _maxCharacterLevel;

    public bool TryLevelUpCharacter()
    {
        if (IsCharacterMaxLevel()) return false;

        var costData = GetCharacterLevelCostData();
        if (costData == null) return false;
        if (_inventory == null) return false;

        if (!HasItem(costData.NeedItem1Id, costData.NeedItem1Count) ||
            !HasItem(costData.NeedItem2Id, costData.NeedItem2Count))
            return false;

        RemoveItem(costData.NeedItem1Id, costData.NeedItem1Count);
        RemoveItem(costData.NeedItem2Id, costData.NeedItem2Count);

        _characterLevel++;
        _statPoints += costData.StatPoints;

        Save();
        OnChanged?.Invoke();
        return true;
    }

    // count만큼 한 번에 소비한다. 강화 UI가 예약해둔(pending) 포인트를 저장 시점에 일괄 반영할 때 쓴다.
    public bool TrySpendPoints(EnhancementStatType statType, int count)
    {
        if (count <= 0) return false;
        if (_statPoints < count) return false;
        if (GetStatLevel(statType) + count > MaxStatLevel) return false;

        _statPoints -= count;
        _statLevels[statType] = GetStatLevel(statType) + count;

        ApplyAll();
        Save();
        OnChanged?.Invoke();
        return true;
    }

    // 가산형이면 레벨*증가량, 배율형이면 1 + 레벨*성장률을 반환한다.
    public float GetBonus(EnhancementStatType statType) => GetBonusAtLevel(statType, GetStatLevel(statType));

    public float GetBonusAtLevel(EnhancementStatType statType, int level)
    {
        var config = _growthConfigs.FirstOrDefault(c => c.statType == statType);
        if (config == null) return statType is EnhancementStatType.AttackPower or EnhancementStatType.AttackSpeed ? 1f : 0f;

        return config.isPercentGrowth ? 1f + level * config.valuePerLevel : level * config.valuePerLevel;
    }

    // 강화 UI가 보여줄 강화 수치 문자열. displayAsPercent면 "+n%"(음수면 "-n%"), 아니면 "+n"(소수면 소숫점 유지).
    public string FormatBonus(EnhancementStatType statType, float bonus)
    {
        var config = _growthConfigs.FirstOrDefault(c => c.statType == statType);
        if (config != null && config.displayAsPercent)
        {
            float percent = config.isPercentGrowth ? (bonus - 1f) * 100f : bonus * 100f;
            int rounded = Mathf.RoundToInt(percent);
            return rounded >= 0 ? $"+{rounded}%" : $"{rounded}%";
        }

        return Mathf.Approximately(bonus, Mathf.Round(bonus))
            ? $"+{Mathf.RoundToInt(bonus)}"
            : $"+{bonus:0.##}";
    }

    private void ApplyAll()
    {
        _playerStat?.ApplyEnhancementStats(
            GetBonus(EnhancementStatType.MaxHp),
            GetBonus(EnhancementStatType.MoveSpeed),
            GetBonus(EnhancementStatType.DefenseRate));

        _playerVision?.SetRangeBonus(GetBonus(EnhancementStatType.VisionRange));

        RefreshEquippedGuns();
    }

    private void RefreshEquippedGuns()
    {
        if (_weaponMount == null) return;
        for (int i = 0; i < 2; i++)
            _weaponMount.GetGun(i)?.RefreshEnhancementMultipliers();
    }

    public CharacterLevelCostData GetCharacterLevelCostData()
    {
        int nextLevel = _characterLevel + 1;
        return CharacterLevelCostTable.Instance.All.FirstOrDefault(d => d.Level == nextLevel);
    }

    private bool HasItem(int itemId, int amount)
    {
        if (itemId <= 0 || amount <= 0) return true;

        var itemData = DataManager.GetItem(itemId);
        return itemData != null && _inventory.HasItem(itemData, amount);
    }

    private void RemoveItem(int itemId, int amount)
    {
        if (itemId <= 0 || amount <= 0) return;

        var itemData = DataManager.GetItem(itemId);
        if (itemData == null) return;

        _inventory.RemoveItem(itemData, amount);
    }

    private void Save()
    {
        SaveManager.Instance?.SaveCharacterEnhancement(_characterLevel, _statPoints, _statLevels);
    }
}
