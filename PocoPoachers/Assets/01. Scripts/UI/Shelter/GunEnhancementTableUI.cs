using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GunEnhancementTableUI : MonoBehaviour
{
    private const float PowerCost = 50f;

    [SerializeField] private GunEnhancementDropHandler _slot;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _statDescText;
    [SerializeField] private TextMeshProUGUI _powerCostText;
    [SerializeField] private Button _enhanceButton;

    [Header("재료 슬롯")]
    [SerializeField] private Transform _ingredientListContent;
    [SerializeField] private IngredientEntryUI _ingredientEntryPrefab;

    private const int MaxLevel = 3;

    private Inventory _inventory;
    private readonly List<IngredientEntryUI> _ingredientEntries = new();

    private void Awake()
    {
        _slot.OnGunSet += _ => Refresh();
        _slot.OnGunCleared += Refresh;
        _enhanceButton.onClick.AddListener(OnClickEnhance);
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
        _inventory = player.PlayerInventory;
        _slot.BindInventoryUI(player.PlayerBagInventoryUI);
        Refresh();
    }

    private void Refresh()
    {
        bool hasItem = _slot.IsSetted;
        _enhanceButton.interactable = false;

        if (!hasItem)
        {
            _levelText.text = "-";
            if (_statDescText != null) _statDescText.text = string.Empty;
            HideAllIngredients();
            RefreshPowerCostUI();
            return;
        }

        int itemId = _slot.DroppedItemData != null ? _slot.DroppedItemData.Id : 0;
        int level = WorldEquipmentManager.GetEnhancementLevel(_slot.DroppedUid, itemId);
        _levelText.text = $"+{level}";

        if (level >= MaxLevel)
        {
            if (_statDescText != null) _statDescText.text = string.Empty;
            HideAllIngredients();
            RefreshPowerCostUI();
            return;
        }

        if (_statDescText != null)
            _statDescText.text = BuildStatDescription(level);

        var cost = GetCostData(level);
        RefreshIngredients(cost);
        RefreshPowerCostUI();
    }

    private static bool HasEnoughPower(float cost) => Generator.Instance != null && Generator.Instance.CurrentPower >= cost;

    // 발전기 전력 잔량에 따라 전력 소비량 표시(초록/빨강)와 강화 버튼 활성화 여부를 갱신
    private void RefreshPowerCostUI()
    {
        bool canAffordPower = HasEnoughPower(PowerCost);

        if (_powerCostText != null)
        {
            _powerCostText.text = string.Format(LocalizationManager.GetInstance().GetString("generator.power_cost_format"), PowerCost.ToString("0"));
            _powerCostText.color = canAffordPower ? UITheme.InkPositive : UITheme.InkNegative;
        }

        if (!_slot.IsSetted)
        {
            _enhanceButton.interactable = false;
            return;
        }

        int itemId = _slot.DroppedItemData != null ? _slot.DroppedItemData.Id : 0;
        int level = WorldEquipmentManager.GetEnhancementLevel(_slot.DroppedUid, itemId);
        if (level >= MaxLevel)
        {
            _enhanceButton.interactable = false;
            return;
        }

        var cost = GetCostData(level);
        _enhanceButton.interactable = CanAfford(cost) && canAffordPower;
    }

    private void OnClickEnhance()
    {
        if (!_slot.IsSetted) return;

        int uid = _slot.DroppedUid;
        int itemId = _slot.DroppedItemData != null ? _slot.DroppedItemData.Id : 0;
        int level = WorldEquipmentManager.GetEnhancementLevel(uid, itemId);
        if (level >= MaxLevel) return;

        var cost = GetCostData(level);
        if (!CanAfford(cost)) return;

        if (Generator.Instance == null || !Generator.Instance.TryConsume(PowerCost))
        {
            var loc = LocalizationManager.GetInstance();
            UIManager.GetInstance().ShowNotice(loc.GetString("generator.title"), loc.GetString("generator.power_insufficient_message"));
            return;
        }

        ConsumeCost(cost);
        WorldEquipmentManager.SetEnhancementLevel(uid, level + 1, itemId);
        // 게스트면 강화 즉시 호스트에 반영 — 장착 여부와 무관하게 강화 상태가 유지된다
        RoomSync.EnhanceItem(uid, itemId, level + 1);
        Refresh();
    }

    // 강화 전후 스탯 변화 텍스트 생성
    private string BuildStatDescription(int currentLevel)
    {
        if (!_slot.IsSetted) return string.Empty;
        var item = _slot.DroppedItemData;
        int nextLevel = currentLevel + 1;
        var sb = new StringBuilder();

        if (item.ItemType == ItemType.GunPart)
        {
            var part = GunPartTable.Instance.Get(item.Id);
            if (part == null) return string.Empty;

            var loc = LocalizationManager.GetInstance();
            AppendMultiplierLine(sb, loc.GetString("stat.spread"), part.SpreadMultiplier, currentLevel, nextLevel);
            AppendMultiplierLine(sb, loc.GetString("stat.aim_speed"), part.AimFovMultiplier, currentLevel, nextLevel);
            AppendMultiplierLine(sb, loc.GetString("stat.reload_speed"), part.ReloadTimeMultiplier, currentLevel, nextLevel);
            AppendMultiplierLine(sb, loc.GetString("stat.recoil"), part.RecoilMultiplier, currentLevel, nextLevel);
            AppendMultiplierLine(sb, loc.GetString("stat.sound_range"), part.SoundRangeMultiplier, currentLevel, nextLevel);

            if (part.MaxMagazineBonus != 0)
            {
                int cur = EnhanceAdditive(part.MaxMagazineBonus, currentLevel);
                int nxt = EnhanceAdditive(part.MaxMagazineBonus, nextLevel);
                sb.AppendLine($"{loc.GetString("stat.magazine_capacity")}: +{cur} → +{nxt}");
            }
        }
        else
        {
            var stat = ArmorStatTable.Instance.Get(item.Id);
            if (stat == null) return string.Empty;

            var loc = LocalizationManager.GetInstance();
            if (stat.DefenseRate > 0)
            {
                float cur = stat.DefenseRate * (1f + 0.1f * currentLevel);
                float nxt = stat.DefenseRate * (1f + 0.1f * nextLevel);
                sb.AppendLine($"{loc.GetString("stat.defense_rate")}: {cur * 100f:F0}% → {nxt * 100f:F0}%");
            }
            if (stat.MaxHpBonus > 0)
            {
                float cur = stat.MaxHpBonus * (1f + 0.1f * currentLevel);
                float nxt = stat.MaxHpBonus * (1f + 0.1f * nextLevel);
                sb.AppendLine($"{loc.GetString("stat.hp_bonus")}: +{cur:F0} → +{nxt:F0}");
            }
            AppendMultiplierLine(sb, loc.GetString("stat.move_speed"), stat.MoveSpeedMultiplier, currentLevel, nextLevel);
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendMultiplierLine(StringBuilder sb, string label, float baseVal, int curLv, int nxtLv)
    {
        if (Mathf.Approximately(baseVal, 1f)) return;
        float cur = EnhanceMultiplier(baseVal, curLv);
        float nxt = EnhanceMultiplier(baseVal, nxtLv);
        sb.AppendLine($"{label}: {FormatPct(cur)} → {FormatPct(nxt)}");
    }

    // WorldEquipmentManager의 공식과 동일
    private static float EnhanceMultiplier(float baseVal, int level)
        => 1f + (baseVal - 1f) * (1f + 0.1f * level);

    private static int EnhanceAdditive(int baseVal, int level)
        => Mathf.RoundToInt(baseVal * (1f + 0.1f * level));

    // 1.0 기준 배율을 %로 표시. 0.89 → "-11%", 1.1 → "+10%"
    private static string FormatPct(float value)
    {
        int pct = Mathf.RoundToInt((value - 1f) * 100f);
        return pct >= 0 ? $"+{pct}%" : $"{pct}%";
    }

    private ItemEnhancementCostData GetCostData(int currentLevel)
    {
        if (!_slot.IsSetted) return null;
        int itemId = _slot.DroppedItemData.Id;
        return ItemEnhancementCostTable.Instance.All
            .FirstOrDefault(d => d.ItemId == itemId && d.Level == currentLevel + 1);
    }

    // 엔트리는 파괴하지 않고 재사용한다 — 슬롯/레벨이 바뀔 때마다 Destroy/Instantiate가 반복되면 GC 스파이크가 생긴다.
    private void RefreshIngredients(ItemEnhancementCostData cost)
    {
        int used = 0;
        var ingredients = cost != null ? cost.NeedItems : null;

        for (int n = 0; ingredients != null && n < ingredients.Count; n++)
        {
            var (itemId, required) = ingredients[n];
            var item = ItemTable.Instance.Get(itemId);
            if (item == null) continue;

            if (used == _ingredientEntries.Count)
                _ingredientEntries.Add(Instantiate(_ingredientEntryPrefab, _ingredientListContent));

            var entry = _ingredientEntries[used];
            used++;
            if (entry == null) continue;

            if (!entry.gameObject.activeSelf) entry.gameObject.SetActive(true);
            entry.transform.SetSiblingIndex(used - 1);   // 표시 순서 유지
            entry.Setup(item, _inventory != null ? _inventory.GetItemCount(item) : 0, required);
        }

        for (int i = used; i < _ingredientEntries.Count; i++)
        {
            var entry = _ingredientEntries[i];
            if (entry != null && entry.gameObject.activeSelf) entry.gameObject.SetActive(false);
        }
    }

    private void HideAllIngredients() => RefreshIngredients(null);

    private bool CanAfford(ItemEnhancementCostData cost)
    {
        if (_inventory == null || cost == null) return false;
        foreach (var (itemId, required) in cost.NeedItems)
        {
            var item = ItemTable.Instance.Get(itemId);
            if (item == null || _inventory.GetItemCount(item) < required) return false;
        }
        return true;
    }

    private void ConsumeCost(ItemEnhancementCostData cost)
    {
        if (cost == null) return;

        foreach (var (itemId, required) in cost.NeedItems)
        {
            var item = ItemTable.Instance.Get(itemId);
            if (item != null) _inventory.RemoveItem(item, required);
        }
    }
}
