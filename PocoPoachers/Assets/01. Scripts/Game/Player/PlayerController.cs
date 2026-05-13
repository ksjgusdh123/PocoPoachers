using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameObject PlayerBagUI;
    [SerializeField] private GameObject boxUI;
    [SerializeField] private CameraController _cameraController;
    private Inventory _inventory;
    private PlayerInputHandler _inputHander;
    private QuickSlotDropHandler[] _quickSlots;
    private GameObject _interactObject;

    private void Start()
    {
        _inventory = GetComponent<Inventory>();
        _quickSlots = FindObjectsByType<QuickSlotDropHandler>(FindObjectsInactive.Include)
            .OrderBy(s => s.gameObject.name).ToArray();

        _inputHander = GetComponent<PlayerInputHandler>();
        _inputHander.GoInventory += ShowInventory;
        _inputHander.RegisterItemNumberKey += RegisterItem;
        _inputHander.ConsumeItemNumberKey += ConsumeItem;
        _inputHander.StartInteraction += Interaction;
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
                _inventory.InteractionInventory = inven;
                inven.InteractionInventory = _inventory;
                boxUI.GetComponentInChildren<InventoryUI>()?.Bind(inven);
                boxUI.GetComponent<ItemBoxRevealUI>().Open(inven);
                _inputHander.SwitchInputActionMap(PlayerInputMapType.Inventory);
            }
            else
            {
                _inventory.InteractionInventory = null;
                inven.InteractionInventory = null;
                _inputHander.SwitchInputActionMap(PlayerInputMapType.Game);
                UIManager.GetInstance().ChangeMouseCursor(true);
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
        UIManager.GetInstance().ChangeMouseCursor(false);
    }

    void RegisterItem(int index)
    {   
        _quickSlots[index].TryRegisterItem();
    }

    void ConsumeItem(int index)
    {
        if (!_quickSlots[index].IsSetted) return;
        _quickSlots[index].GetComponent<ItemConsume>().ConsumeItem();
    }
}
