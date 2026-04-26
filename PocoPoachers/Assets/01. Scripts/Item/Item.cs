using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private int _itemId;

    public ItemData Data => ItemTable.Instance.Get(_itemId);
}
