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
        _quickSlotData.ConsumeItem();
    }
}
