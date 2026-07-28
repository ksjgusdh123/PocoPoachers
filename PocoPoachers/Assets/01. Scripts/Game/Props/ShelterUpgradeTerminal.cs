using UnityEngine;

public class ShelterUpgradeTerminal : MonoBehaviour, IInteractable
{
    public void OnInteract(PlayerController player)
    {
        player.LockCamera(true);
        player.SwitchInputMap(PlayerInputMapType.ItemBox);

        var storage = FindAnyObjectByType<Storage>();
        FindAnyObjectByType<ShelterUpgradeUI>(FindObjectsInactive.Include)
            ?.Open(storage?.StorageInventory, player.PlayerInventory);

        UIManager.GetInstance().Show(UIType.ShelterUpgrade);
    }

    public void OnInteractExit(PlayerController player)
    {
        UIManager.GetInstance().Hide(UIType.ShelterUpgrade);
        player.LockCamera(false);
        player.SwitchToGameplayInputMap();
    }
}
