using UnityEngine;

public enum ItemType
{
    Weapon,
    Armor,
    Consumable,
    Miscellaneous
}

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [SerializeField] private string _itemName;
    [SerializeField] private string _description;
    [SerializeField] private Sprite _icon;
    [SerializeField] private ItemType _itemType;
    [SerializeField] private int _maxStack = 1;

    public string ItemName => _itemName;
    public string Description => _description;
    public Sprite Icon => _icon;
    public ItemType ItemType => _itemType;
    public int MaxStack => _maxStack;
}
