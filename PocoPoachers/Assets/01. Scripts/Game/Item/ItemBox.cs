using UnityEngine;

public class ItemBox : MonoBehaviour
{
    public int[] ItemIds { get; private set; }

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

}
