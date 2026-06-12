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
        // 아직 공개되지 않은(리빌 진행 중) 박스 슬롯은 설명 미표시
        if (slot.InventoryUI != null && slot.InventoryUI.IsSlotUnrevealed(slot.SlotIndex)) return;
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        transform.position = slot.transform.position + _offset;
        _name.text = slot.SlotItemData.ItemName;
        _description.text = slot.SlotItemData.Description;
    }

    public void HideDescription()
    {
        _name.text = "";
        _description.text = "";
        gameObject.SetActive(false);
    }
}
