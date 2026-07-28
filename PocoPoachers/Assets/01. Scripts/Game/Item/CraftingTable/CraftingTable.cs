using UnityEngine;

public class CraftingTable : MonoBehaviour, IInteractable
{
    public void OnInteract(PlayerController player)
    {
        UIManager.GetInstance().Show(UIType.CraftingTable);

        var ui = player.GetCraftingTableUI;
        ui?.SendMessage("Open", player, SendMessageOptions.DontRequireReceiver);

        player.SwitchInputMap(PlayerInputMapType.ItemBox);
    }

    public void OnInteractExit(PlayerController player)
    {
        UIManager.GetInstance().Hide(UIType.CraftingTable);
        player.SwitchToGameplayInputMap();
        UIManager.GetInstance().ChangeMouseCursor(true);
    }
}
