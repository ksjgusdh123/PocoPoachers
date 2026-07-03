using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RepairWorkbenchUI : MonoBehaviour
{
    [SerializeField] private RepairSlotDropHandler _repairSlot;
    [SerializeField] private TextMeshProUGUI _durabilityText;
    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private Button _repairButton;

    private Inventory _player;

    private void Awake()
    {
        _repairSlot.OnItemSet += OnItemSet;
        _repairSlot.OnItemCleared += OnItemCleared;
        _repairButton.onClick.AddListener(OnClickRepair);
    }

    public void Open(PlayerController player)
    {
        _player = player.PlayerInventory;
        _repairSlot.BindInventoryUI(player.PlayerBagInventoryUI);
        Refresh();
    }

    private void OnItemSet(ItemData data)
    {
        Refresh();
    }

    private void OnItemCleared()
    {
        Refresh();
    }

    private void Refresh()
    {
        bool hasItem = _repairSlot.IsSetted;

        _repairButton.interactable = hasItem;

        if (!hasItem)
        {
            _durabilityText.text = "- / -";
            _costText.text = "-";
            return;
        }

        if (WorldEquipmentManager.TryGetDurability(_repairSlot.DroppedUid, out float cur, out float max))
            _durabilityText.text = $"{cur:F0} / {max:F0}";
        else
            _durabilityText.text = "? / ?";
        _costText.text = BuildCostText(_repairSlot.DroppedItemData);
    }

    private string BuildCostText(ItemData itemData)
    {
        var cost = RepairCostTable.Instance.All.FirstOrDefault(d => d.ItemId == itemData.Id);
        if (cost == null) return "-";

        var sb = new StringBuilder();
        AppendItemRow(sb, cost.NeedItem1Id, cost.NeedItem1Count);
        AppendItemRow(sb, cost.NeedItem2Id, cost.NeedItem2Count);

        return sb.Length > 0 ? sb.ToString() : "-";
    }

    private void AppendItemRow(StringBuilder sb, int itemId, int required)
    {
        if (itemId == 0 || required <= 0) return;

        var itemData = ItemTable.Instance.Get(itemId);
        string name = itemData != null ? LocalizationManager.GetInstance().GetString(itemData.Name) : $"ID:{itemId}";
        int current = _player != null ? _player.GetItemCount(itemData) : 0;

        if (sb.Length > 0) sb.Append('\n');
        sb.Append($"{name} {current} / {required}");
    }

    private void OnClickRepair()
    {
        if (!_repairSlot.IsSetted) return;

        // TODO: 수리 로직 구현
    }
}
