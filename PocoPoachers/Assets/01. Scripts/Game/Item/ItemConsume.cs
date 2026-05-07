using UnityEngine;

public class ItemConsume : MonoBehaviour
{
    QuickSlotDropHandler _quickSlotData;

    private void Awake()
    {
        _quickSlotData = GetComponent<QuickSlotDropHandler>();
    }

    public void ConsumeItem()
    {
        PacketBuilder.Send(new C_ConsumeItemT
        {
            ItemId = _quickSlotData._droppedItemData.id,
            Amount = _quickSlotData._droppedAmount,
            InventoryIndex = 0,
        }, C_ConsumeItem.Pack, PacketType.C_ConsumeItem);
        _quickSlotData.ConsumeItem();
    }

}
