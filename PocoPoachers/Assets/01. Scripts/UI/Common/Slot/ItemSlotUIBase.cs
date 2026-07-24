using TMPro;
using UnityEngine;

public abstract class ItemSlotUIBase : SlotUIBase
{
    [SerializeField] protected TextMeshProUGUI _nameText;
    [SerializeField] protected TextMeshProUGUI _amountText;

    protected void SetDisplay(ItemData data, int amount)
    {
        SetIcon(data);
        if (_nameText != null)
            _nameText.text = data != null ? LocalizationManager.GetInstance().GetString(data.ItemName) : "";
        // 스택 최대치가 1인 아이템(무기/방어구/파츠 등)은 개수 표시가 무의미하므로 숨긴다
        if (_amountText != null)
            _amountText.text = data != null && amount >= 1 && data.MaxStack > 1 ? amount.ToString() : "";
    }

    protected void ClearDisplay() => SetDisplay(null, 0);
}
