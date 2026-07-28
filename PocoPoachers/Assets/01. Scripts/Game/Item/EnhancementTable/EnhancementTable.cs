using UnityEngine;

public class EnhancementTable : MonoBehaviour, IInteractable
{
    public void OnInteract(PlayerController player)
    {
        //SoundManager.GetInstance().PlaySfx("sfx_storage_open");

        UIManager.GetInstance().Show(UIType.EnhancementTable);

        var enhancementTableUI = player.GetEnhancementTableUI;
        enhancementTableUI?.SendMessage("Open", player, SendMessageOptions.DontRequireReceiver);

        player.SwitchInputMap(PlayerInputMapType.ItemBox);
    }

    public void OnInteractExit(PlayerController player)
    {
        //SoundManager.GetInstance().PlaySfx("sfx_storage_close");

        UIManager.GetInstance().Hide(UIType.EnhancementTable);

        player.SwitchToGameplayInputMap();
        UIManager.GetInstance().ChangeMouseCursor(true);
    }
}
