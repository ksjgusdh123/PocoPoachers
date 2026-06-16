using UnityEngine;

public class Spaceship : MonoBehaviour, IInteractable
{
    public void OnInteract(PlayerController player)
    {
        player.LockCamera(true);
        player.SwitchInputMap(PlayerInputMapType.ItemBox);
        UIManager.GetInstance().Show(UIType.PlanetSelect);
    }

    public void OnInteractExit(PlayerController player)
    {
        UIManager.GetInstance().Hide(UIType.PlanetSelect);
        player.LockCamera(false);
        player.SwitchInputMap(PlayerInputMapType.Game);
    }
}
