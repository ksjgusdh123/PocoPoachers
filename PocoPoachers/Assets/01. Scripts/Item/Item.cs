using UnityEngine;

[System.Serializable]
public class Item
{
    [SerializeField] private ItemData _data;

    public ItemData Data => _data;

    public Item(ItemData data)
    {
        _data = data;
    }
}
