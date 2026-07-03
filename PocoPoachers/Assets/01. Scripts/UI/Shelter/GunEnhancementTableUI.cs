using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GunEnhancementTableUI : MonoBehaviour
{
    [SerializeField] private GunEnhancementDropHandler _slot;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private Button _enhanceButton;

    [Header("재료 슬롯 (최대 2개)")]
    [SerializeField] private GameObject[] _ingredientRows;
    [SerializeField] private Image[] _ingredientIcons;
    [SerializeField] private TextMeshProUGUI[] _ingredientNameTexts;
    [SerializeField] private TextMeshProUGUI[] _ingredientCountTexts;

    private const int MaxLevel = 3;
    private const string CostStatKey = "ItemEnhancement";

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
            HideAllIngredients();
            return;
        }

        int level = WorldEquipmentManager.GetEnhancementLevel(_slot.DroppedUid);
        _levelText.text = $"+{level}";

        if (level >= MaxLevel)
        {
            HideAllIngredients();
            return;
        }

        var cost = GetCostData(level);
        RefreshIngredients(cost);
        _enhanceButton.interactable = CanAfford(cost);
    }

    private void OnClickEnhance()
    {
        if (!_slot.IsSetted) return;

        int uid = _slot.DroppedUid;
        int level = WorldEquipmentManager.GetEnhancementLevel(uid);
        if (level >= MaxLevel) return;

        var cost = GetCostData(level);
        if (!CanAfford(cost)) return;

        ConsumeCost(cost);
        WorldEquipmentManager.SetEnhancementLevel(uid, level + 1);
        Refresh();
    }

    private EnhancementCostData GetCostData(int currentLevel) =>
        EnhancementCostTable.Instance.All.FirstOrDefault(d => d.Stat == CostStatKey && d.Level == currentLevel + 1);

    private void RefreshIngredients(EnhancementCostData cost)
    {
        var ingredients = GetIngredients(cost);

        for (int i = 0; i < _ingredientRows.Length; i++)
        {
            bool active = i < ingredients.Length;
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

    private bool CanAfford(EnhancementCostData cost)
    {
        if (_inventory == null || cost == null) return false;
        foreach (var (itemId, required) in GetIngredients(cost))
        {
            var item = ItemTable.Instance.Get(itemId);
            if (item == null || _inventory.GetItemCount(item) < required) return false;
        }
        return true;
    }

    private void ConsumeCost(EnhancementCostData cost)
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

    private static (int itemId, int count)[] GetIngredients(EnhancementCostData cost)
    {
        if (cost == null) return System.Array.Empty<(int, int)>();

        var list = new System.Collections.Generic.List<(int, int)>();
        if (cost.NeedItem1Id > 0) list.Add((cost.NeedItem1Id, cost.NeedItem1Count));
        if (cost.NeedItem2Id > 0) list.Add((cost.NeedItem2Id, cost.NeedItem2Count));
        return list.ToArray();
    }
}
