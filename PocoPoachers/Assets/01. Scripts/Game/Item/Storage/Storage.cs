using UnityEngine;

/// <summary>
/// 창고 오브젝트. ItemBox와 달리 Reveal 연출 없이 바로 인벤토리 UI를 열어준다.
/// </summary>
public class Storage : MonoBehaviour, IInteractable
{
    public void OnInteract(PlayerController player)
    {
        var inven = GetComponent<Inventory>();

        player.SetInventoryOpen(true);

        var storageUI = player.GetStorageUI;
        storageUI.SetActive(true);
        storageUI.GetComponentInChildren<InventoryUI>()?.Bind(inven);

        player.PlayerInventory.InteractionInventory = inven;
        inven.InteractionInventory = player.PlayerInventory;

        player.SwitchInputMap(PlayerInputMapType.ItemBox);
    }

    public void OnInteractExit(PlayerController player)
    {
        var inven = GetComponent<Inventory>();

        player.PlayerInventory.InteractionInventory = null;
        inven.InteractionInventory = null;

        player.GetStorageUI.SetActive(false);
        player.SetInventoryOpen(false);

        player.SwitchInputMap(PlayerInputMapType.Game);
        UIManager.GetInstance().ChangeMouseCursor(true);
    }
}
