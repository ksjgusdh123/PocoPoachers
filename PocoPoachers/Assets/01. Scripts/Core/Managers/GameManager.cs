using UnityEngine;
using UnityEngine.UI;

public class GameManager : Singleton<GameManager>
{
    public Inventory GainedInventory { get; private set; }
    public Inventory GiveInventory { get; private set; }

    public bool ShouldLoadPlayerInventory { get; private set; }

    public void SetLoadPlayerInventory(bool load) => ShouldLoadPlayerInventory = load;

    public void SaveChangeInventorys(Inventory give, Inventory gained)
    {
        GiveInventory = give;
        GainedInventory = gained;
    }
}
