using UnityEngine;

public class ItemBox : MonoBehaviour
{
    [SerializeField] private ProximityDetector _proximityDetector;

    public int[] ItemIds { get; private set; }

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

    private void ShowPulse()
    {
        _pulseUI = WorldUIManager.Instance.Create<UIScalePulse>(WorldUIType.ScalePulse, transform);
    }

    private void HidePulse()
    {
        _pulseUI?.Release();
        _pulseUI = null;
    }
}
