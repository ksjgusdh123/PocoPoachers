using UnityEngine;

public class GunEnhancementTable : MonoBehaviour, IInteractable
{
    public void OnInteract(PlayerController player)
    {
        player.SetInventoryOpen(true);
        UIManager.GetInstance().Show(UIType.GunEnhancementTable);

        var ui = player.GetGunEnhancementTableUI;
        ui?.SendMessage("Open", player, SendMessageOptions.DontRequireReceiver);

        player.SwitchInputMap(PlayerInputMapType.ItemBox);
    }

    public void OnInteractExit(PlayerController player)
    {
        UIManager.GetInstance().Hide(UIType.GunEnhancementTable);
        player.SetInventoryOpen(false);

        player.SwitchToGameplayInputMap();
        UIManager.GetInstance().ChangeMouseCursor(true);
    }
}
