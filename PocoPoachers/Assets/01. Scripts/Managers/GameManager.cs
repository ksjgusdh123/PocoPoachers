using UnityEngine;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    private void Start()
    {
        // Temp
        var inventory = GameObject.Find("Cube").GetComponent<Inventory>();

        Item[] items = FindObjectsByType<Item>();
        foreach (var item in items)
        {
            if (item.Data != null) inventory.AddItem(item.Data, 1);
        }

        var otherInven = GameObject.Find("Box").GetComponent<Inventory>();

        foreach (var item in items)
        {
            if (item.Data != null) otherInven.AddItem(item.Data, 1);
        }
    }
}
