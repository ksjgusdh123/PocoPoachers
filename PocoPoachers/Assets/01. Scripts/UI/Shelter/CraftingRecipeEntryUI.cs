using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CraftingRecipeEntryUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;

    private CraftingRecipeData _recipe;
    private Action<CraftingRecipeData> _onSelected;

    private void Awake()
    {
        GetComponent<Button>()?.onClick.AddListener(OnClick);
    }

    public void Setup(CraftingRecipeData recipe, Action<CraftingRecipeData> onSelected)
    {
        _recipe = recipe;
        _onSelected = onSelected;

        var item = ItemTable.Instance.Get(recipe.ResultItemId);
        if (item == null) return;

        if (_icon != null)
            _icon.sprite = ResourceManager.Instance.LoadSprite(item.icon);
        if (_nameText != null)
            _nameText.text = LocalizationManager.GetInstance().GetString(item.name);
    }

    private void OnClick() => _onSelected?.Invoke(_recipe);
}
