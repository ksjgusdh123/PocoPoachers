using UnityEngine;

public class ItemBox : MonoBehaviour, IInteractable
{
    [SerializeField] private ProximityDetector _proximityDetector;

    public int[] ItemIds { get; private set; }
    public bool HasBeenOpened { get; private set; }

    private UIScalePulse _pulseUI;

    private void Awake()
    {
        _proximityDetector.OnEnter += ShowPulse;
        _proximityDetector.OnExit += HidePulse;
    }

    public void Initialize(int[] itemIds)
    {
        ItemIds = itemIds;

        if (!gameObject.TryGetComponent<Inventory>(out var inven))
            inven = gameObject.AddComponent<Inventory>();

        foreach (int id in itemIds)
        {
            ItemData data = ItemTable.Instance.Get(id);
            if (data == null) continue;
            int slotIndex = inven.CanAddItem(data, 1);
            if (slotIndex >= 0) inven.AddItemAtSlot(slotIndex, data, 1);
        }
    }

    public void OnInteract(PlayerController player)
    {
        var inven = GetComponent<Inventory>();

        player.SetInventoryOpen(true);

        var boxUI = player.BoxUI;
        boxUI.SetActive(true);
        boxUI.GetComponentInChildren<InventoryUI>()?.Bind(inven);
        boxUI.GetComponent<ItemBoxRevealUI>()?.Open(inven);

        player.PlayerInventory.InteractionInventory = inven;
        inven.InteractionInventory = player.PlayerInventory;

        MarkOpened();
        GetComponent<ItemBoxAnimation>()?.SetOpen(true);
        player.SwitchInputMap(PlayerInputMapType.ItemBox);
    }

    public void OnInteractExit(PlayerController player)
    {
        var inven = GetComponent<Inventory>();

        player.PlayerInventory.InteractionInventory = null;
        inven.InteractionInventory = null;

        player.BoxUI.SetActive(false);
        player.SetInventoryOpen(false);

        GetComponent<ItemBoxAnimation>()?.SetOpen(false);
        player.SwitchInputMap(PlayerInputMapType.Game);
        UIManager.GetInstance().ChangeMouseCursor(true);
    }

    public void MarkOpened()
    {
        HasBeenOpened = true;
        HidePulse();
    }

    private void ShowPulse()
    {
        _pulseUI = WorldUIManager.Instance.Create<UIScalePulse>(WorldUIType.ScalePulse, transform);
        _pulseUI.SetVisited(HasBeenOpened);
    }

    private void HidePulse()
    {
        _pulseUI?.Release();
        _pulseUI = null;
    }
}
