using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GunEnhancementTableUI : MonoBehaviour
{
    [SerializeField] private GunEnhancementDropHandler _slot;
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private Button _enhanceButton;

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
            _costText.text = "-";
            return;
        }

        int level = WorldEquipmentManager.GetEnhancementLevel(_slot.DroppedUid);
        _levelText.text = $"+{level}";

        if (level >= MaxLevel)
        {
            _costText.text = "MAX";
            return;
        }

        _costText.text = BuildCostText(level);
        _enhanceButton.interactable = CanAfford(level);
    }

    private void OnClickEnhance()
    {
        if (!_slot.IsSetted) return;

        int uid = _slot.DroppedUid;
        int level = WorldEquipmentManager.GetEnhancementLevel(uid);
        if (level >= MaxLevel || !CanAfford(level)) return;

        ConsumeCost(level);
        WorldEquipmentManager.SetEnhancementLevel(uid, level + 1);
        Refresh();
    }

    private EnhancementCostData GetCostData(int currentLevel) =>
        EnhancementCostTable.Instance.All.FirstOrDefault(d => d.Stat == CostStatKey && d.Level == currentLevel + 1);

    private bool CanAfford(int level)
    {
        if (_inventory == null) return false;
        var cost = GetCostData(level);
        if (cost == null) return false;
        return HasItem(cost.NeedItem1Id, cost.NeedItem1Count)
            && HasItem(cost.NeedItem2Id, cost.NeedItem2Count);
    }

    private bool HasItem(int itemId, int count)
    {
        if (itemId <= 0 || count <= 0) return true;
        var data = ItemTable.Instance.Get(itemId);
        return data != null && _inventory.HasItem(data, count);
    }

    private void ConsumeCost(int level)
    {
        var cost = GetCostData(level);
        if (cost == null) return;
        RemoveItem(cost.NeedItem1Id, cost.NeedItem1Count);
        RemoveItem(cost.NeedItem2Id, cost.NeedItem2Count);
    }

    private void RemoveItem(int itemId, int count)
    {
        if (itemId <= 0 || count <= 0) return;
        var data = ItemTable.Instance.Get(itemId);
        if (data == null) return;
        _inventory.RemoveItem(data, count);
    }

    private string BuildCostText(int level)
    {
        var cost = GetCostData(level);
        if (cost == null) return "-";
        var sb = new StringBuilder();
        AppendCostRow(sb, cost.NeedItem1Id, cost.NeedItem1Count);
        AppendCostRow(sb, cost.NeedItem2Id, cost.NeedItem2Count);
        return sb.Length > 0 ? sb.ToString() : "-";
    }

    private void AppendCostRow(StringBuilder sb, int itemId, int count)
    {
        if (itemId <= 0 || count <= 0) return;
        var data = ItemTable.Instance.Get(itemId);
        string name = data != null ? LocalizationManager.GetInstance().GetString(data.ItemName) : $"ID:{itemId}";
        int have = _inventory != null ? _inventory.GetItemCount(data) : 0;
        if (sb.Length > 0) sb.Append('\n');
        sb.Append($"{name} {have} / {count}");
    }
}
