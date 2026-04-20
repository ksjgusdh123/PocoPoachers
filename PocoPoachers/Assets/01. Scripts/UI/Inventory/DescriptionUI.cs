using TMPro;
using UnityEngine;

public class DescriptionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _name;
    [SerializeField] private TextMeshProUGUI _description;
    [SerializeField] private Vector3 _offset;

    public void ShowDescription(ItemSlotUI slot)
    {
        if (!slot.IsSettedItem) return;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        transform.position = slot.transform.position + _offset;
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
