using UnityEngine;

public class ItemBox : MonoBehaviour
{
    public int[] ItemIds { get; private set; }

    [SerializeField] private LayerMask _playerLayer;

    private UIScalePulse _pulseUI;

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

    private void OnTriggerEnter(Collider other)
    {
        if ((_playerLayer & (1 << other.gameObject.layer)) == 0) return;
        _pulseUI = WorldUIManager.Instance.Create<UIScalePulse>(WorldUIType.ScalePulse, transform);
    }

    private void OnTriggerExit(Collider other)
    {
        if ((_playerLayer & (1 << other.gameObject.layer)) == 0) return;
        _pulseUI?.Release();
        _pulseUI = null;
    }
}
