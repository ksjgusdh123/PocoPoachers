using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private ItemSlotUI _slotPrefab;
    [SerializeField] private Transform _slotParent;
    [SerializeField] private Button _sortButton;
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private TextMeshProUGUI _miniCountText;

    protected ItemSlotUI[] _slotUIs;
    private DescriptionUI _descriptionUI;

    public Inventory Inventory => _inventory;
    public WorldObject Box => _inventory.GetComponent<WorldObject>();
    public bool IsBox => _inventory.TryGetComponent<WorldObject>(out _);

    protected virtual void Awake()
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
            foreach (var slot in _inventory.Slots)
                slot.OnChanged += RefreshCountText;
            if (_slotUIs == null)
                GenerateSlots();
            Refresh();
        }

        if (_sortButton) _sortButton.onClick.AddListener(OnClickSort);

        var manager = SlotInteractionManager.GetInstance();
        manager.OnHoverEnter += _descriptionUI.ShowDescription;
        manager.OnHoverExit += _ => _descriptionUI.HideDescription();
    }

    public void OnSlotDoubleClicked()
    {
        var targetSlot = SlotInteractionManager.GetInstance().HoveredSlot;
        if (targetSlot == null) return;

        var target = _inventory.InteractionInventory;
        ItemData itemData = targetSlot.SlotItemData;
        int amount = targetSlot.SavedAmountItem;

        int addedSlotIndex = target?.CanAddItem(itemData, amount) ?? -1;
        if (addedSlotIndex < 0) return;

        GameManager.GetInstance().SaveChangeInventorys(_inventory, target);

        Inventory boxInventory = _inventory.TryGetComponent<WorldObject>(out _) ? _inventory : target;
        bool isNetworked = boxInventory.TryGetComponent<WorldObject>(out var boxWo);

        if (isNetworked && !(RoomManager.IsHost))
        {
            // 낙관적 업데이트: 즉시 로컬 적용 후 호스트에 요청
            bool playerGains = boxInventory == _inventory;
            target.AddItemAtSlot(addedSlotIndex, itemData, amount);
            _inventory.RemoveItemAtSlot(targetSlot.SlotIndex, itemData, amount);
            RoomSync.ItemGain(playerGains, boxWo.Id, itemData.id, amount, addedSlotIndex, targetSlot.SlotIndex);
        }
        else
        {
            // 호스트 또는 싱글플레이: 로컬에서 바로 적용
            target.AddItemAtSlot(addedSlotIndex, itemData, amount);
            _inventory.RemoveItemAtSlot(targetSlot.SlotIndex, itemData, amount);
            RoomSync.ItemBoxUpdate(boxWo.Id, itemData.id, boxInventory != _inventory ? amount : -amount, boxInventory != _inventory ? -1 : targetSlot.SlotIndex);
        }
    }

    public void OnSlotDropped()
    {
        GameManager.GetInstance().SaveChangeInventorys(_inventory, _inventory.InteractionInventory);
    }

    public void Bind(Inventory inventory)
    {
        if (_inventory != null)
        {
            _inventory.ChangeInventory -= Refresh;
            foreach (var slot in _inventory.Slots)
                slot.OnChanged -= RefreshCountText;
        }
        _inventory = inventory;
        _inventory.ChangeInventory += Refresh;
        foreach (var slot in _inventory.Slots)
            slot.OnChanged += RefreshCountText;
        if (_slotUIs == null)
        {
            GenerateSlots();
            _descriptionUI ??= FindAnyObjectByType<DescriptionUI>(FindObjectsInactive.Include);
        }
        Refresh();
    }

    private void OnDestroy()
    {
        if (_inventory == null) return;
        _inventory.ChangeInventory -= Refresh;
        foreach (var slot in _inventory.Slots)
            slot.OnChanged -= RefreshCountText;
    }

    private void OnClickSort()
    {
        _inventory.Sort();
    }

    // 최대 용량만큼 슬롯 UI 생성
    protected virtual void GenerateSlots()
    {
        _slotUIs = new ItemSlotUI[_inventory.MaxCapacity];

        for (int i = 0; i < _inventory.MaxCapacity; i++)
        {
            _slotUIs[i] = Instantiate(_slotPrefab, _slotParent);
            _slotUIs[i].SetIndex(i);
        }

        _descriptionUI.HideDescription();
    }

    // 인벤토리 데이터 기반으로 전체 UI 갱신
    public virtual void Refresh()
    {
        int current = _inventory.CurrentCapacity;

        for (int i = 0; i < _slotUIs.Length; i++)
        {
            bool isActive = i < current;
            _slotUIs[i].gameObject.SetActive(isActive);

            if (isActive)
                _slotUIs[i].SetSlot(_inventory.Slots[i]);
        }

        RefreshCountText();
    }

    // 아이템 수 / 용량 텍스트 갱신
    protected virtual void RefreshCountText()
    {
        if (_countText != null)
        {
            _countText.text = $"({_inventory.ItemCount} / {_inventory.CurrentCapacity})";
            _miniCountText.text = _countText.text;
        }
    }
}
