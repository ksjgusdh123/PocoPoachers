using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GunEnhancementTableUI : MonoBehaviour
{
    [SerializeField] private GunEnhancementDropHandler _slot;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _statDescText;
    [SerializeField] private Button _enhanceButton;

    [Header("재료 슬롯 (최대 2개)")]
    [SerializeField] private GameObject[] _ingredientRows;
    [SerializeField] private Image[] _ingredientIcons;
    [SerializeField] private TextMeshProUGUI[] _ingredientNameTexts;
    [SerializeField] private TextMeshProUGUI[] _ingredientCountTexts;

    private const int MaxLevel = 3;

    private Inventory _inventory;

    private void Awake()
    {
        _slot.OnGunSet += _ => Refresh();
        _slot.OnGunCleared += Refresh;
        _enhanceButton.onClick.AddListener(OnClickEnhance);
    }

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
            return;
        }

        int itemId = _slot.DroppedItemData != null ? _slot.DroppedItemData.Id : 0;
        int level = WorldEquipmentManager.GetEnhancementLevel(_slot.DroppedUid, itemId);
        _levelText.text = $"+{level}";

        if (level >= MaxLevel)
        {
            if (_statDescText != null) _statDescText.text = string.Empty;
            HideAllIngredients();
            return;
        }

        if (_statDescText != null)
            _statDescText.text = BuildStatDescription(level);

        var cost = GetCostData(level);
        RefreshIngredients(cost);
        _enhanceButton.interactable = CanAfford(cost);
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

        ConsumeCost(cost);
        WorldEquipmentManager.SetEnhancementLevel(uid, level + 1, itemId);
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

            AppendMultiplierLine(sb, "산탄 확산", part.SpreadMultiplier, currentLevel, nextLevel);
            AppendMultiplierLine(sb, "조준 속도", part.AimFovMultiplier, currentLevel, nextLevel);
            AppendMultiplierLine(sb, "재장전 속도", part.ReloadTimeMultiplier, currentLevel, nextLevel);
            AppendMultiplierLine(sb, "반동", part.RecoilMultiplier, currentLevel, nextLevel);
            AppendMultiplierLine(sb, "소음 범위", part.SoundRangeMultiplier, currentLevel, nextLevel);

            if (part.MaxMagazineBonus != 0)
            {
                int cur = EnhanceAdditive(part.MaxMagazineBonus, currentLevel);
                int nxt = EnhanceAdditive(part.MaxMagazineBonus, nextLevel);
                sb.AppendLine($"탄창 용량: +{cur} → +{nxt}");
            }
        }
        else
        {
            var stat = ArmorStatTable.Instance.Get(item.Id);
            if (stat == null) return string.Empty;

            if (stat.DefenseRate > 0)
            {
                float cur = stat.DefenseRate * (1f + 0.1f * currentLevel);
                float nxt = stat.DefenseRate * (1f + 0.1f * nextLevel);
                sb.AppendLine($"방어율: {cur * 100f:F0}% → {nxt * 100f:F0}%");
            }
            if (stat.MaxHpBonus > 0)
            {
                float cur = stat.MaxHpBonus * (1f + 0.1f * currentLevel);
                float nxt = stat.MaxHpBonus * (1f + 0.1f * nextLevel);
                sb.AppendLine($"HP 보너스: +{cur:F0} → +{nxt:F0}");
            }
            AppendMultiplierLine(sb, "이동속도", stat.MoveSpeedMultiplier, currentLevel, nextLevel);
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

    private void RefreshIngredients(ItemEnhancementCostData cost)
    {
        var ingredients = GetIngredients(cost);

        for (int i = 0; i < _ingredientRows.Length; i++)
        {
            bool active = i < ingredients.Count;
            _ingredientRows[i].SetActive(active);
            if (!active) continue;

            var (itemId, required) = ingredients[i];
            var item = ItemTable.Instance.Get(itemId);
            if (item == null) continue;

            _ingredientIcons[i].sprite = ResourceManager.Instance.LoadSprite(item.icon);
            _ingredientNameTexts[i].text = LocalizationManager.GetInstance().GetString(item.ItemName);

            int owned = _inventory?.GetItemCount(item) ?? 0;
            _ingredientCountTexts[i].text = $"{owned} / {required}";
            _ingredientCountTexts[i].color = owned >= required ? Color.green : Color.red;
        }
    }

    private void HideAllIngredients()
    {
        foreach (var row in _ingredientRows)
            row.SetActive(false);
    }

    private bool CanAfford(ItemEnhancementCostData cost)
    {
        if (_inventory == null || cost == null) return false;
        foreach (var (itemId, required) in GetIngredients(cost))
        {
            var item = ItemTable.Instance.Get(itemId);
            if (item == null || _inventory.GetItemCount(item) < required) return false;
        }
        return true;
    }

    private void ConsumeCost(ItemEnhancementCostData cost)
    {
        if (cost == null) return;
        RemoveItem(cost.NeedItem1Id, cost.NeedItem1Count);
        RemoveItem(cost.NeedItem2Id, cost.NeedItem2Count);
    }

    private void RemoveItem(int itemId, int count)
    {
        if (itemId <= 0 || count <= 0) return;
        var item = ItemTable.Instance.Get(itemId);
        if (item == null) return;
        _inventory.RemoveItem(item, count);
    }

    private static List<(int itemId, int count)> GetIngredients(ItemEnhancementCostData cost)
    {
        var list = new List<(int, int)>();
        if (cost == null) return list;
        if (cost.NeedItem1Id > 0) list.Add((cost.NeedItem1Id, cost.NeedItem1Count));
        if (cost.NeedItem2Id > 0) list.Add((cost.NeedItem2Id, cost.NeedItem2Count));
        return list;
    }
}
