using UnityEngine;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    private void Start()
    {
        FindAnyObjectByType<Inventory>().AddItem(FindAnyObjectByType<Item>());
    }
}
 