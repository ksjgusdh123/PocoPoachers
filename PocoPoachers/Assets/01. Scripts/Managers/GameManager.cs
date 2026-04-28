using UnityEngine;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    public Inventory GainedInventory { get; private set; }
    public Inventory GiveInventory { get; private set; }

    public void SaveChangeInventorys(Inventory give, Inventory gained)
    {
        GiveInventory = give;
        GainedInventory = gained;
    }
}
