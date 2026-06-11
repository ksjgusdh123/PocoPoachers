using TMPro;
using UnityEngine;

public abstract class ItemSlotUIBase : SlotUIBase
{
    [SerializeField] protected TextMeshProUGUI _nameText;
    [SerializeField] protected TextMeshProUGUI _amountText;

    protected void SetDisplay(ItemData data, int amount)
    {
        SetIcon(data);
        _nameText.text = data != null ? data.ItemName : "";
        _amountText.text = data != null && amount >= 1 ? amount.ToString() : "";
    }

    protected void ClearDisplay() => SetDisplay(null, 0);
}
