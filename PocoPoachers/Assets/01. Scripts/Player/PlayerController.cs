using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameObject PlayerBagUI;
    private void Start()
    {
        GetComponent<PlayerInputHandler>().GoInventory -= ToggleInventory;
        GetComponent<PlayerInputHandler>().GoInventory += ToggleInventory;
    }

    private void ToggleInventory()
    {
        if (PlayerBagUI.activeSelf) PlayerBagUI.SetActive(false);
        else PlayerBagUI.SetActive(true);
    }
}
