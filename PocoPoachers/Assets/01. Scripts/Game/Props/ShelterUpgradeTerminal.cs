using UnityEngine;

public class ShelterUpgradeTerminal : MonoBehaviour, IInteractable
{
    public void OnInteract(PlayerController player)
    {
        player.LockCamera(true);
        player.SwitchInputMap(PlayerInputMapType.ItemBox);
        UIManager.GetInstance().Show(UIType.ShelterUpgrade);

        var storage = FindAnyObjectByType<Storage>();
        FindAnyObjectByType<ShelterUpgradeUI>(FindObjectsInactive.Include)?.Open(storage?.StorageInventory);
    }

    public void OnInteractExit(PlayerController player)
    {
        UIManager.GetInstance().Hide(UIType.ShelterUpgrade);
        player.LockCamera(false);
        player.SwitchInputMap(PlayerInputMapType.Game);
    }
}
