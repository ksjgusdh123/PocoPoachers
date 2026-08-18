using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RepairWorkbenchUI : MonoBehaviour
{
    private const float PowerCost = 30f;

    [SerializeField] private RepairSlotDropHandler _repairSlot;
    [SerializeField] private TextMeshProUGUI _durabilityText;
    [SerializeField] private TextMeshProUGUI _powerCostText;

    [Header("Cost - Ingredients")]
    [SerializeField] private Transform _ingredientListContent;
    [SerializeField] private IngredientEntryUI _ingredientEntryPrefab;

    [SerializeField] private Button _repairButton;

    private Inventory _player;
    private readonly List<IngredientEntryUI> _ingredientEntries = new();

    private void Awake()
    {
        _repairSlot.OnItemSet += OnItemSet;
        _repairSlot.OnItemCleared += OnItemCleared;
        _repairButton.onClick.AddListener(OnClickRepair);
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
        if (!_repairSlot.IsSetted)
        {
            _durabilityText.text = "- / -";
            RefreshIngredients(null);
            RefreshPowerCostUI();
            return;
        }

        if (WorldEquipmentManager.TryGetDurability(_repairSlot.DroppedUid, out float cur, out float max))
            _durabilityText.text = $"{cur:F0} / {max:F0}";
        else
            _durabilityText.text = "? / ?";

        RefreshIngredients(FindCost(_repairSlot.DroppedItemData));

        RefreshPowerCostUI();
    }

    // 엔트리는 파괴하지 않고 재사용한다 — 슬롯을 바꿀 때마다 Destroy/Instantiate가 반복되면 GC 스파이크가 생긴다.
    private void RefreshIngredients(RepairCostData cost)
    {
        int used = 0;
        var ingredients = cost != null ? cost.NeedItems : null;

        for (int n = 0; ingredients != null && n < ingredients.Count; n++)
        {
            var (itemId, required) = ingredients[n];
            var mat = ItemTable.Instance.Get(itemId);
            if (mat == null) continue;

            if (used == _ingredientEntries.Count)
                _ingredientEntries.Add(Instantiate(_ingredientEntryPrefab, _ingredientListContent));

            var entry = _ingredientEntries[used];
            used++;
            if (entry == null) continue;

            if (!entry.gameObject.activeSelf) entry.gameObject.SetActive(true);
            entry.transform.SetSiblingIndex(used - 1);   // 표시 순서 유지
            entry.Setup(mat, _player != null ? _player.GetItemCount(mat) : 0, required);
        }

        for (int i = used; i < _ingredientEntries.Count; i++)
        {
            var entry = _ingredientEntries[i];
            if (entry != null && entry.gameObject.activeSelf) entry.gameObject.SetActive(false);
        }
    }

    private void RefreshPowerCostUI()
    {
        bool canAffordPower = Generator.Instance != null && Generator.Instance.CurrentPower >= PowerCost;

        if (_powerCostText != null)
        {
            _powerCostText.text = string.Format(LocalizationManager.GetInstance().GetString("generator.power_cost_format"), PowerCost.ToString("0"));
            _powerCostText.color = canAffordPower ? UITheme.InkPositive : UITheme.InkNegative;
        }

        _repairButton.interactable = _repairSlot.IsSetted && canAffordPower;
    }

    private static RepairCostData FindCost(ItemData itemData)
    {
        return itemData == null ? null : RepairCostTable.Instance.All.FirstOrDefault(d => d.ItemId == itemData.Id);
    }

    private void OnClickRepair()
    {
        if (!_repairSlot.IsSetted) return;

        var itemData = _repairSlot.DroppedItemData;
        var cost = FindCost(itemData);
        if (cost == null) return;
        if (!CanRepair(cost)) return;

        int uid = _repairSlot.DroppedUid;
        if (!WorldEquipmentManager.TryGetDurability(uid, out float cur, out float max)) return;
        if (cur >= max) return;

        if (Generator.Instance == null || !Generator.Instance.TryConsume(PowerCost))
        {
            var loc = LocalizationManager.GetInstance();
            UIManager.GetInstance().ShowNotice(loc.GetString("generator.title"), loc.GetString("generator.power_insufficient_message"));
            return;
        }

        ConsumeRepairCost(cost);

        float restoreAmount = max - cur;
        WorldEquipmentManager.ApplyChange(uid, itemData.Id, restoreAmount, max);
        RoomSync.Durability(uid, itemData.Id, restoreAmount, max);

        Refresh();
    }

    private bool CanRepair(RepairCostData cost)
    {
        if (_player == null) return false;

        foreach (var (itemId, required) in cost.NeedItems)
        {
            var item = ItemTable.Instance.Get(itemId);
            if (item == null || !_player.HasItem(item, required)) return false;
        }
        return true;
    }

    private void ConsumeRepairCost(RepairCostData cost)
    {
        foreach (var (itemId, required) in cost.NeedItems)
        {
            var item = ItemTable.Instance.Get(itemId);
            if (item != null) _player.RemoveItem(item, required);
        }
    }
}
