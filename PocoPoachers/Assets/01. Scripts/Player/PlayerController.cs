using System.Linq;
using UnityEngine;

// 부수적인 플레이어 관리 클래스
public class PlayerController : MonoBehaviour
{
    [SerializeField] private GameObject PlayerBagUI;
    private Inventory _inventory;
    private DropQuickSlotUI[] _quickSlots;
    private GameObject _interactObject;

    private void Start()
    {
        _inventory = GetComponent<Inventory>();
        _quickSlots = FindObjectsByType<DropQuickSlotUI>(FindObjectsInactive.Include)
            .OrderBy(s => s.gameObject.name).ToArray();

        var inputHandler = GetComponent<PlayerInputHandler>();
        inputHandler.GoInventory += ShowInventory;
        inputHandler.ItemNumberKey += RegisterItem;
        inputHandler.StartInteraction += Interaction;
    }

    private void OnTriggerEnter(Collider other)
    {
        _interactObject = other.gameObject;
    }

    private void OnTriggerExit(Collider other)
    {
        _interactObject = null;
    }

    void Interaction()
    {
        // temp
        if (_interactObject.TryGetComponent<Inventory>(out var inven))
        {
            ShowInventory();
            _inventory._interactionInventory = inven;
            inven._interactionInventory = _inventory;
        }
    }

    void ShowInventory()
    {
        PlayerBagUI.SetActive(!PlayerBagUI.activeSelf);
    }

    void RegisterItem(int index)
    {
        _quickSlots[index].TryRegisterItem();
    }
}
