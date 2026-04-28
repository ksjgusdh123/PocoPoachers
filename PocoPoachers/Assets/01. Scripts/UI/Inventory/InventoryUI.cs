using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private ItemSlotUI _slotPrefab;
    [SerializeField] private Transform _slotParent;
    [SerializeField] private Button _sortButton;

    private ItemSlotUI[] _slotUIs;
    private HashSet<ItemSlotUI> _slotSet;
    private DescriptionUI _descriptionUI;

    public WorldObject Box => _inventory.GetComponent<WorldObject>();
    public bool IsBox => _inventory.TryGetComponent<WorldObject>(out _);


    private void Start()
    {
        _descriptionUI ??= FindAnyObjectByType<DescriptionUI>(FindObjectsInactive.Include);

        if (_inventory != null)
        {
            _inventory.ChangeInventory += Refresh;
            if (_slotUIs == null)
                GenerateSlots();
            Refresh();
        }

        if (_sortButton) _sortButton.onClick.AddListener(OnClickSort);

        var manager = SlotInteractionManager.GetInstance();
        manager.OnHoverEnter += _descriptionUI.ShowDescription;
        manager.OnHoverExit += _descriptionUI.HideDescription;
        manager.OnDoubleClick += OnSlotDoubleClicked;
    }

    private void OnSlotDoubleClicked()
    {
        var targetSlot = SlotInteractionManager.GetInstance().HoveredSlot;
        if (targetSlot == null) return;

        if (!_slotSet.Contains(targetSlot)) return;

        var target = _inventory._interactionInventory;
        if (target == null || !target.CanAddItem(targetSlot.SlotItemData)) return;

        int boxUid;
        bool isPlayer;
        if (_inventory.TryGetComponent<WorldObject>(out var worldObject))
        {
             boxUid = worldObject.Id;
            isPlayer = false;
        }
        else
        {
            boxUid = target.GetComponent<WorldObject>().Id;
            isPlayer = true;
        }
        int itemTypeId = targetSlot.SlotItemData.Id;
        GameManager.GetInstance().SaveChangeInventorys(_inventory, target);
        PacketSender.CGainItemReq(boxUid, itemTypeId, targetSlot.SavedAmountItem, isPlayer);

        //if (target.AddItem(targetSlot.SlotItemData, targetSlot.SavedAmountItem))
        //    targetSlot.ClearSlot();
    }

    public void OnSlotDropped()
    {
        GameManager.GetInstance().SaveChangeInventorys(_inventory, _inventory._interactionInventory);
    }

    public void Bind(Inventory inventory)
    {
        if (_inventory != null)
            _inventory.ChangeInventory -= Refresh;
        _inventory = inventory;
        _inventory.ChangeInventory += Refresh;
        if (_slotUIs == null)
        {
            _descriptionUI ??= FindAnyObjectByType<DescriptionUI>(FindObjectsInactive.Include);
            GenerateSlots();
        }
        Refresh();
    }

    private void OnClickSort()
    {
        _inventory.Sort();
    }

    // 최대 용량만큼 슬롯 UI 생성
    private void GenerateSlots()
    {
        _slotUIs = new ItemSlotUI[_inventory.MaxCapacity];
        _slotSet = new HashSet<ItemSlotUI>();

        for (int i = 0; i < _inventory.MaxCapacity; i++)
        {
            _slotUIs[i] = Instantiate(_slotPrefab, _slotParent);
            _slotSet.Add(_slotUIs[i]);
        }

        _descriptionUI.HideDescription(null);
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
