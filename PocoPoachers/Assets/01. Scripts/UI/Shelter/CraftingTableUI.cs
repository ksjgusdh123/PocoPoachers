using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 좌측: 카테고리 탭 + 스크롤 레시피 목록
// 우측: 선택된 아이템 상세 (아이콘, 이름, 설명, 재료, 제작 버튼)
public class CraftingTableUI : MonoBehaviour
{
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
        RefreshList();
    }

    private void RefreshList()
    {
        foreach (var entry in _entries)
        {
            if (entry != null) Destroy(entry.gameObject);
        }
        _entries.Clear();

        foreach (var recipe in CraftingRecipeTable.Instance.All)
        {
            var item = ItemTable.Instance.Get(recipe.ResultItemId);
            if (item == null || item.type != _selectedCategory) continue;

            var entry = Instantiate(_recipeEntryPrefab, _recipeListContent);
            entry.Setup(recipe, OnRecipeSelected);
            _entries.Add(entry);
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
            _ingredientCountTexts[i].color = owned >= required ? Color.green : Color.red;
        }

        _craftButton.interactable = CanCraft(_selectedRecipe);
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

    private void OnClickCraft()
    {
        if (_selectedRecipe == null || !CanCraft(_selectedRecipe)) return;

        var resultItem = ItemTable.Instance.Get(_selectedRecipe.ResultItemId);
        if (resultItem == null) return;
        if (_inventory.CanAddItem(resultItem, _selectedRecipe.ResultCount) < 0) return;

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
