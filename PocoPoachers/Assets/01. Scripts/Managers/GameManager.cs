using UnityEngine;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    private void Start()
    {
        var inventory = FindAnyObjectByType<Inventory>();

        Item[] items =FindObjectsByType<Item>();
        foreach (var item in items)
        {
            inventory.AddItem(item.Data, 1);
        }
    }
}
 