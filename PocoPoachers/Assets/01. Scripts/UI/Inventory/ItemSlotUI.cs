using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] private Image _icon;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private GameObject _emptyOverlay; // 빈 슬롯 표시용

    public void SetSlot(ItemSlot slot)
    {
        if (slot.IsEmpty)
        {
            SetEmpty();
            return;
        }

        _icon.sprite = slot.Item.Data.Icon;
        _icon.enabled = true;
        _nameText.text = slot.Item.Data.ItemName;
        _amountText.text = slot.Amount >= 1 ? slot.Amount.ToString() : "";

        if (_emptyOverlay != null)
            _emptyOverlay.SetActive(false);
    }

    private void SetEmpty()
    {
        _icon.sprite = null;
        //_icon.enabled = false;
        _nameText.text = "";
        _amountText.text = "";

        if (_emptyOverlay != null)
            _emptyOverlay.SetActive(true);
    }
}
