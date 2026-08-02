using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 좌측: 카테고리 탭 + 스크롤 레시피 목록
// 우측: 선택된 아이템 상세 (아이콘, 이름, 설명, 재료, 제작 버튼)
public class CraftingTableUI : MonoBehaviour
{
    private const float PowerCost = 20f;

    [Header("Category")]
    [SerializeField] private Button[] _categoryButtons;
    [SerializeField] private ItemType[] _categoryTypes;

    [Header("Recipe List")] 
    [SerializeField] private Transform _recipeListContent;
    [SerializeField] private CraftingRecipeEntryUI _recipeEntryPrefab;

    [Header("Detail - Result")]
    [SerializeField] private GameObject _detailPanel;
    [SerializeField] private Image _resultIcon;
    [SerializeField] private TextMeshProUGUI _resultNameText;
    [SerializeField] private TextMeshProUGUI _resultDescriptionText;
    [SerializeField] private TextMeshProUGUI _resultCountText;

    [Header("Detail - Ingredients (최대 3개)")]
    [SerializeField] private GameObject[] _ingredientRows;
    [SerializeField] private Image[] _ingredientIcons;
    [SerializeField] private TextMeshProUGUI[] _ingredientNameTexts;
    [SerializeField] private TextMeshProUGUI[] _ingredientCountTexts;

    [Header("Craft")]
    [SerializeField] private Button _craftButton;
    [SerializeField] private TextMeshProUGUI _powerCostText;

    private PlayerController _player;
    private Inventory _inventory;
    private CraftingRecipeData _selectedRecipe;
    private ItemType _selectedCategory;
    private readonly List<CraftingRecipeEntryUI> _entries = new();

    private void Awake()
    {
        for (int i = 0; i < _categoryButtons.Length; i++)
        {
            int captured = i;
            _categoryButtons[i].onClick.AddListener(() => SelectCategory(_categoryTypes[captured]));
        }
        _craftButton?.onClick.AddListener(OnClickCraft);
        _detailPanel?.SetActive(false);
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
        _player = player;
        _inventory = player?.PlayerInventory;
        _selectedRecipe = null;
        _detailPanel?.SetActive(false);

        for (int i = 0; i < _categoryButtons.Length; i++)
            _categoryButtons[i].gameObject.SetActive(true);

        // 첫 번째 유효한 카테고리 자동 선택
        for (int i = 0; i < _categoryButtons.Length; i++)
        {
            if (_categoryButtons[i].gameObject.activeSelf)
            {
                SelectCategory(_categoryTypes[i]);
                break;
            }
        }
    }

    public void SelectCategory(ItemType type)
    {
        _selectedCategory = type;
        _selectedRecipe = null;
        _detailPanel?.SetActive(false);
        RefreshCategorySelection();
        RefreshList();
    }

    private void RefreshCategorySelection()
    {
        Color selectedColor = UITheme.InkPrimary;
        Color normalColor = UITheme.InkSecondary;

        for (int i = 0; i < _categoryButtons.Length; i++)
        {
            Button button = _categoryButtons[i];
            if (button == null) continue;

            bool selected = i < _categoryTypes.Length && _categoryTypes[i] == _selectedCategory;
            Transform accent = button.transform.Find("SelectionAccent");
            if (accent != null) accent.gameObject.SetActive(selected);

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.color = selected ? selectedColor : normalColor;
        }
    }

    // 엔트리는 파괴하지 않고 재사용한다 — 카테고리를 전환할 때마다 전체 Destroy/Instantiate가
    // 반복되면서 GC 스파이크가 생긴다.
    private void RefreshList()
    {
        int used = 0;

        foreach (var recipe in CraftingRecipeTable.Instance.All)
        {
            var item = ItemTable.Instance.Get(recipe.ResultItemId);
            if (item == null || item.type != _selectedCategory) continue;

            if (used == _entries.Count)
                _entries.Add(Instantiate(_recipeEntryPrefab, _recipeListContent));

            var entry = _entries[used];
            used++;
            if (entry == null) continue;

            if (!entry.gameObject.activeSelf) entry.gameObject.SetActive(true);
            entry.transform.SetSiblingIndex(used - 1);   // 표시 순서 유지
            entry.Setup(recipe, OnRecipeSelected);
        }

        // 이번 카테고리에서 쓰이지 않은 나머지는 비활성화만 한다.
        for (int i = used; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry != null && entry.gameObject.activeSelf) entry.gameObject.SetActive(false);
        }
    }

    private void OnRecipeSelected(CraftingRecipeData recipe)
    {
        _selectedRecipe = recipe;
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        if (_selectedRecipe == null)
        {
            _detailPanel?.SetActive(false);
            return;
        }

        _detailPanel?.SetActive(true);

        var resultItem = ItemTable.Instance.Get(_selectedRecipe.ResultItemId);
        if (resultItem == null) return;

        _resultIcon.sprite = ResourceManager.Instance.LoadSprite(resultItem.icon);
        _resultNameText.text = LocalizationManager.GetInstance().GetString(resultItem.name);
        _resultDescriptionText.text = LocalizationManager.GetInstance().GetString(resultItem.description);
        _resultCountText.text = $"x{_selectedRecipe.ResultCount}";

        var ingredients = GetIngredients(_selectedRecipe);
        for (int i = 0; i < _ingredientRows.Length; i++)
        {
            bool active = i < ingredients.Count;
            _ingredientRows[i].SetActive(active);
            if (!active) continue;

            var (itemId, required) = ingredients[i];
            var mat = ItemTable.Instance.Get(itemId);
            if (mat == null) continue;

            _ingredientIcons[i].sprite = ResourceManager.Instance.LoadSprite(mat.icon);
            _ingredientNameTexts[i].text = LocalizationManager.GetInstance().GetString(mat.name);

            int owned = _inventory?.GetItemCount(mat) ?? 0;
            _ingredientCountTexts[i].text = $"{owned} / {required}";
            _ingredientCountTexts[i].color = owned >= required ? UITheme.InkPositive : UITheme.InkNegative;
        }

        RefreshPowerCostUI();
    }

    private void RefreshPowerCostUI()
    {
        bool canAffordPower = HasEnoughPower(PowerCost);

        if (_powerCostText != null)
        {
            _powerCostText.text = string.Format(LocalizationManager.GetInstance().GetString("generator.power_cost_format"), PowerCost.ToString("0"));
            _powerCostText.color = canAffordPower ? UITheme.InkPositive : UITheme.InkNegative;
        }

        if (_selectedRecipe != null)
            _craftButton.interactable = CanCraft(_selectedRecipe) && canAffordPower;
    }

    private bool CanCraft(CraftingRecipeData recipe)
    {
        if (_inventory == null) return false;
        foreach (var (itemId, required) in GetIngredients(recipe))
        {
            var mat = ItemTable.Instance.Get(itemId);
            if (mat == null || _inventory.GetItemCount(mat) < required) return false;
        }
        return true;
    }

    private static bool HasEnoughPower(float cost) => Generator.Instance != null && Generator.Instance.CurrentPower >= cost;

    private void OnClickCraft()
    {
        if (_selectedRecipe == null || !CanCraft(_selectedRecipe)) return;

        var resultItem = ItemTable.Instance.Get(_selectedRecipe.ResultItemId);
        if (resultItem == null) return;
        if (_inventory.CanAddItem(resultItem, _selectedRecipe.ResultCount) < 0) return;

        if (Generator.Instance == null || !Generator.Instance.TryConsume(PowerCost))
        {
            var loc = LocalizationManager.GetInstance();
            UIManager.GetInstance().ShowNotice(loc.GetString("generator.title"), loc.GetString("generator.power_insufficient_message"));
            return;
        }

        foreach (var (itemId, required) in GetIngredients(_selectedRecipe))
            _inventory.RemoveItem(ItemTable.Instance.Get(itemId), required);

        _inventory.AddItem(resultItem, _selectedRecipe.ResultCount);
        RefreshDetail();
    }

    private bool HasRecipesOfType(ItemType type)
    {
        foreach (var recipe in CraftingRecipeTable.Instance.All)
        {
            var item = ItemTable.Instance.Get(recipe.ResultItemId);
            if (item != null && item.type == type) return true;
        }
        return false;
    }

    private static List<(int itemId, int count)> GetIngredients(CraftingRecipeData recipe)
    {
        var list = new List<(int, int)>();
        if (recipe.NeedItem1Id > 0) list.Add((recipe.NeedItem1Id, recipe.NeedItem1Count));
        if (recipe.NeedItem2Id > 0) list.Add((recipe.NeedItem2Id, recipe.NeedItem2Count));
        if (recipe.NeedItem3Id > 0) list.Add((recipe.NeedItem3Id, recipe.NeedItem3Count));
        return list;
    }
}
