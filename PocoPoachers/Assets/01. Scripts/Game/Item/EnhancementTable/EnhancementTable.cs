using UnityEngine;

public class EnhancementTable : MonoBehaviour, IInteractable
{
    public void OnInteract(PlayerController player)
    {
        //SoundManager.GetInstance().PlaySfx("sfx_storage_open");

        var enhancementTableUI = player.GetEnhancementTableUI;
        enhancementTableUI?.SendMessage("Open", player, SendMessageOptions.DontRequireReceiver);

        UIManager.GetInstance().Show(UIType.EnhancementTable);

        player.SwitchInputMap(PlayerInputMapType.ItemBox);
    }

    public void OnInteractExit(PlayerController player)
    {
        //SoundManager.GetInstance().PlaySfx("sfx_storage_close");

        UIManager.GetInstance().Hide(UIType.EnhancementTable);

        player.SwitchInputMap(PlayerInputMapType.Game);
        UIManager.GetInstance().ChangeMouseCursor(true);
    }
}
