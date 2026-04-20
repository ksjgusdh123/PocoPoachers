using UnityEngine;

// 부수적인 플레이어 관리 클래스
public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameObject PlayerBagUI;

    bool isHovering;

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
