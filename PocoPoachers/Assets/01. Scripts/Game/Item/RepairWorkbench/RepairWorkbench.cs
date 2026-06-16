using UnityEngine;

public class RepairWorkbench : MonoBehaviour, IInteractable
{
    public void OnInteract(PlayerController player)
    {
        player.SetInventoryOpen(true);
        UIManager.GetInstance().Show(UIType.RepairWorkbench);

        var ui = player.GetRepairWorkbenchUI;
        ui?.SendMessage("Open", player, SendMessageOptions.DontRequireReceiver);

        player.SwitchInputMap(PlayerInputMapType.ItemBox);
    }

    public void OnInteractExit(PlayerController player)
    {
        UIManager.GetInstance().Hide(UIType.RepairWorkbench);
        player.SetInventoryOpen(false);

        player.SwitchInputMap(PlayerInputMapType.Game);
        UIManager.GetInstance().ChangeMouseCursor(true);
    }
}
