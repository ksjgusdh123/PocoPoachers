using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 기체 레벨업 전용 패널 — 레벨, 재료(아이콘+이름+개수), 레벨업 버튼만 다룬다. 스탯 강화는 EnhancementStatUI가 담당.
public class EnhancementLevelUpUI : MonoBehaviour
{
    private const float LevelUpPowerCost = 50f;

    [SerializeField] private TextMeshProUGUI _characterLevelText;
    [SerializeField] private TextMeshProUGUI _levelUpPowerCostText;
    [SerializeField] private Button _levelUpButton;

    [Header("재료 슬롯 (최대 2개)")]
    [SerializeField] private GameObject[] _ingredientRows;
    [SerializeField] private Image[] _ingredientIcons;
    [SerializeField] private TextMeshProUGUI[] _ingredientNameTexts;
    [SerializeField] private TextMeshProUGUI[] _ingredientCountTexts;

    private PlayerEnhancement _playerEnhancement;
    private Inventory _inventory;

    private void Awake()
    {
        _levelUpButton?.onClick.AddListener(OnClickLevelUp);
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

    private void HandlePowerChanged(float current, float max) => Refresh();

    public void Open(PlayerEnhancement playerEnhancement, Inventory inventory)
    {
        _playerEnhancement = playerEnhancement;
        _inventory = inventory;
        Refresh();
    }

    public void Refresh()
    {
        if (_playerEnhancement == null)
        {
            if (_characterLevelText != null) _characterLevelText.text = "-";
            if (_levelUpButton != null) _levelUpButton.interactable = false;
            HideAllIngredients();
            return;
        }

        if (_characterLevelText != null)
            _characterLevelText.text = $"Lv. {_playerEnhancement.CharacterLevel} / {_playerEnhancement.MaxCharacterLevel}";

        if (_playerEnhancement.IsCharacterMaxLevel())
        {
            HideAllIngredients();
            if (_levelUpButton != null) _levelUpButton.interactable = false;
            RefreshPowerCostUI(canAffordPower: false);
            return;
        }

        var cost = _playerEnhancement.GetCharacterLevelCostData();
        RefreshIngredients(cost);

        bool canAffordPower = Generator.Instance != null && Generator.Instance.CurrentPower >= LevelUpPowerCost;
        RefreshPowerCostUI(canAffordPower);

        if (_levelUpButton != null)
            _levelUpButton.interactable = CanAfford(cost) && canAffordPower;
    }

    private void RefreshPowerCostUI(bool canAffordPower)
    {
        if (_levelUpPowerCostText != null)
        {
            _levelUpPowerCostText.text = string.Format(LocalizationManager.GetInstance().GetString("generator.power_cost_format"), LevelUpPowerCost.ToString("0"));
            _levelUpPowerCostText.color = canAffordPower ? UITheme.InkPositive : UITheme.InkNegative;
        }
    }

    private void RefreshIngredients(CharacterLevelCostData cost)
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
            _ingredientCountTexts[i].color = owned >= required ? UITheme.InkPositive : UITheme.InkNegative;
        }
    }

    private void HideAllIngredients()
    {
        foreach (var row in _ingredientRows)
            row.SetActive(false);
    }

    private bool CanAfford(CharacterLevelCostData cost)
    {
        if (_inventory == null || cost == null) return false;
        foreach (var (itemId, required) in GetIngredients(cost))
        {
            var item = ItemTable.Instance.Get(itemId);
            if (item == null || _inventory.GetItemCount(item) < required) return false;
        }
        return true;
    }

    private static List<(int itemId, int count)> GetIngredients(CharacterLevelCostData cost)
    {
        var list = new List<(int, int)>();
        if (cost == null) return list;
        if (cost.NeedItem1Id > 0) list.Add((cost.NeedItem1Id, cost.NeedItem1Count));
        if (cost.NeedItem2Id > 0) list.Add((cost.NeedItem2Id, cost.NeedItem2Count));
        return list;
    }

    private void OnClickLevelUp()
    {
        if (_playerEnhancement == null) return;

        if (Generator.Instance == null || Generator.Instance.CurrentPower < LevelUpPowerCost)
        {
            var loc = LocalizationManager.GetInstance();
            UIManager.GetInstance().ShowNotice(loc.GetString("generator.title"), loc.GetString("generator.power_insufficient_message"));
            return;
        }

        if (!_playerEnhancement.TryLevelUpCharacter()) return;

        Generator.Instance.TryConsume(LevelUpPowerCost);
        Refresh();
    }
}
