using UnityEngine;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private ItemSlotUI _slotPrefab;
    [SerializeField] private Transform _slotParent;

    private ItemSlotUI[] _slotUIs;

    private void Start()
    {
        _inventory.ChangeInventory += Refresh;
        GenerateSlots();
        Refresh();
    }

    // 최대 용량만큼 슬롯 UI 생성
    private void GenerateSlots()
    {
        _slotUIs = new ItemSlotUI[_inventory.MaxCapacity];

        for (int i = 0; i < _inventory.MaxCapacity; i++)
        {
            _slotUIs[i] = Instantiate(_slotPrefab, _slotParent);
        }
    }

    // 인벤토리 데이터 기반으로 전체 UI 갱신
    public void Refresh()
    {
        int current = _inventory.CurrentCapacity;

        for (int i = 0; i < _slotUIs.Length; i++)
        {
            bool isActive = i < current;
            _slotUIs[i].gameObject.SetActive(isActive);

            if (isActive)
                _slotUIs[i].SetSlot(_inventory.Slots[i]);
        }
    }
}
