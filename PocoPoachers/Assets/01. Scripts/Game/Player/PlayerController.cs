using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameObject PlayerBagUI;
    [SerializeField] private GameObject boxUI;
    [SerializeField] private CameraController _cameraController;
    private Inventory _inventory;
    private QuickSlotDropHandler[] _quickSlots;
    private GameObject _interactObject;

    private void Start()
    {
        _inventory = GetComponent<Inventory>();
        _quickSlots = FindObjectsByType<QuickSlotDropHandler>(FindObjectsInactive.Include)
            .OrderBy(s => s.gameObject.name).ToArray();

        var inputHandler = GetComponent<PlayerInputHandler>();
        inputHandler.GoInventory += ShowInventory;
        inputHandler.ItemNumberKey += RegisterItem;
        inputHandler.StartInteraction += Interaction;
        inputHandler.DoubleClick += () => SlotInteractionManager.GetInstance().InvokeDoubleClick();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject != gameObject) _interactObject = other.gameObject;
    }

    private void OnTriggerExit(Collider other)
    {
        _interactObject = null;
    }

    void Interaction()
    {
        if (_interactObject != null && _interactObject.TryGetComponent<Inventory>(out var inven))
        {
            ShowInventory();
            boxUI.SetActive(!boxUI.activeSelf);
            if (boxUI.activeSelf)
            {
                _inventory._interactionInventory = inven;
                inven._interactionInventory = _inventory;
                boxUI.GetComponentInChildren<InventoryUI>()?.Bind(inven);
                boxUI.GetComponent<ItemBoxRevealUI>().Open();
            }
            else
            {
                _inventory._interactionInventory = null;
                inven._interactionInventory = null;
            }
        }
    }

    public void LockCamera(bool locked) 
    { 
        _cameraController.SetLocked(locked);
    }

    void ShowInventory()
    {
        PlayerBagUI.SetActive(!PlayerBagUI.activeSelf);
        LockCamera(PlayerBagUI.activeSelf);
    }

    void RegisterItem(int index)
    {
        _quickSlots[index].TryRegisterItem();
    }
}
