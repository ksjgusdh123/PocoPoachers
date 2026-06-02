using UnityEngine;

/// <summary>
/// 창고 오브젝트. ItemBox와 달리 Reveal 연출 없이 바로 인벤토리 UI를 열어준다.
/// </summary>
public class Storage : MonoBehaviour, IInteractable
{
    [SerializeField] private string _saveKey = "storage";

    private Inventory _inventory;

    private void Awake()
    {
        _inventory = GetComponent<Inventory>();

        var storageUI = FindAnyObjectByType<StorageUI>(FindObjectsInactive.Include);
        storageUI?.GetComponent<InventoryUI>()?.Bind(_inventory);
        storageUI.gameObject.SetActive(false);

        SaveManager.GetInstance().LoadInventory(_saveKey, _inventory);
    }

    public void OnInteract(PlayerController player)
    {
        player.SetInventoryOpen(true);

        var storageUI = player.GetStorageUI;
        storageUI.SetActive(true);

        player.PlayerInventory.InteractionInventory = _inventory;
        _inventory.InteractionInventory = player.PlayerInventory;

        player.SwitchInputMap(PlayerInputMapType.ItemBox);
    }

    public void OnInteractExit(PlayerController player)
    {
        player.PlayerInventory.InteractionInventory = null;
        _inventory.InteractionInventory = null;

        player.GetStorageUI.SetActive(false);
        player.SetInventoryOpen(false);

        player.SwitchInputMap(PlayerInputMapType.Game);
        UIManager.GetInstance().ChangeMouseCursor(true);

        SaveManager.GetInstance().SaveInventory(_saveKey, _inventory);
    }
}
