using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private ItemSlotUI _slotPrefab;
    [SerializeField] private Transform _slotParent;
    [SerializeField] private Button _sortButton;

    private ItemSlotUI[] _slotUIs;
    private DescriptionUI _descriptionUI;

    public WorldObject Box => _inventory.GetComponent<WorldObject>();
    public bool IsBox => _inventory.TryGetComponent<WorldObject>(out _);

    private void Awake()
    {
        ItemSlotUI[] slots = GetComponentsInChildren<ItemSlotUI>();
        if (slots.Length > 0)
        {
            _slotUIs = slots;
            for (int i = 0; i < slots.Length; i++)
                slots[i].SetIndex(i);
        }

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
    }

    private void Start()
    {
      
    }

    public void OnSlotDoubleClicked()
    {
        var targetSlot = SlotInteractionManager.GetInstance().HoveredSlot;
        if (targetSlot == null) return;

        var target = _inventory.InteractionInventory;
        if (target == null || !target.CanAddItem(targetSlot.SlotItemData)) return;

        ItemData itemData = targetSlot.SlotItemData;
        int amount = targetSlot.SavedAmountItem;

        GameManager.GetInstance().SaveChangeInventorys(_inventory, target);

        Inventory boxInventory = _inventory.TryGetComponent<WorldObject>(out _) ? _inventory : target;
        bool isNetworked = boxInventory.TryGetComponent<WorldObject>(out var boxWo);

        if (isNetworked && !(RoomManager.Instance?.IsHost ?? false))
        {
            // 게스트: 호스트에게 요청 → H_ItemGainResult에서 실제 적용
            PacketBuilder.SendToHost(new G_ItemGainT
            {
                IsPlayerGained = boxInventory == _inventory,
                BoxUid         = boxWo.Id,
                ItemTypeId     = itemData.id,
                Amount         = amount,
                SlotIndex      = targetSlot.SlotIndex
            }, G_ItemGain.Pack, PacketType.G_ItemGain);
        }
        else
        {
            // 호스트 또는 싱글플레이: 로컬에서 바로 적용
            target.AddItem(itemData, amount);
            _inventory.RemoveItemAtSlot(targetSlot.SlotIndex, itemData, amount);
            PacketBuilder.BroadcastToGuests(new H_ItemBoxUpdateT
            {
                BoxUid = boxWo.Id,
                ItemTypeId = itemData.id,
                Amount = boxInventory != _inventory ? amount : -amount,
                SlotIndex = boxInventory != _inventory ? -1 : targetSlot.SlotIndex,
            }, H_ItemBoxUpdate.Pack, PacketType.H_ItemBoxUpdate);
        }
    }

    public void OnDraggedSlot(ItemData data, int amount, int gainedSlotIndex, int removedSlotIndex)
    {
        var target = _inventory.InteractionInventory;
        if (target == null) return;

        int boxUid;
        if (_inventory.TryGetComponent<WorldObject>(out var worldObject))
        {
            boxUid = worldObject.Id;
        }
        else
        {
            boxUid = target.GetComponent<WorldObject>().Id;
        }
        int itemTypeId = data.Id;
        GameManager.GetInstance().SaveChangeInventorys(target, _inventory);
        //TODO: Send Pkt
    }

    public void OnSlotDropped()
    {
        GameManager.GetInstance().SaveChangeInventorys(_inventory, _inventory.InteractionInventory);
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

        for (int i = 0; i < _inventory.MaxCapacity; i++)
        {
            _slotUIs[i] = Instantiate(_slotPrefab, _slotParent);
            _slotUIs[i].SetIndex(i);
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
