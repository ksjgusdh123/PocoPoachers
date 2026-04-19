using TMPro;
using UnityEngine;

public class DescriptionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _description;

    public void ShowDescription(ItemSlotUI slot)
    {
        if (!slot.IsSettedItem) return;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        _name.text = slot.SlotItemData.ItemName;
        _description.text = slot.SlotItemData.Description;
    }

    public void HideDescription(ItemSlotUI slot)
    {
        _name.text = null;
        _description.text = null;
        gameObject.SetActive(false);
    }
}
