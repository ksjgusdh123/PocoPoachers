using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DropQuickSlotUI : DropSlotUI
{
    [SerializeField] private TextMeshProUGUI _countText;
    [SerializeField] private int _quickSlotCount;

    protected override bool OnItemDropped(ItemData data, int amount)
    {
        if(base.OnItemDropped(data, amount))
        {
            _countText.text = amount.ToString();
            return true;
        }
        return false;
    }
}
