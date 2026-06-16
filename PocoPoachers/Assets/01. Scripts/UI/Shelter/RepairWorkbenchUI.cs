using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RepairWorkbenchUI : MonoBehaviour
{
    [SerializeField] private RepairSlotDropHandler _repairSlot;
    [SerializeField] private TextMeshProUGUI _durabilityText;
    [SerializeField] private TextMeshProUGUI _costText;
    [SerializeField] private Button _repairButton;

    private void Awake()
    {
        _repairSlot.OnItemSet += OnItemSet;
        _repairSlot.OnItemCleared += OnItemCleared;
        _repairButton.onClick.AddListener(OnClickRepair);
    }

    public void Open(PlayerController player)
    {
        Refresh();
    }

    private void OnItemSet(ItemData data)
    {
        Refresh();
    }

    private void OnItemCleared()
    {
        Refresh();
    }

    private void Refresh()
    {
        bool hasItem = _repairSlot.IsSetted;

        _repairButton.interactable = hasItem;

        if (!hasItem)
        {
            _durabilityText.text = "- / -";
            _costText.text = "-";
            return;
        }

        // TODO: ItemSlot 내구도 시스템 구현 후 실제 값으로 교체
        _durabilityText.text = "? / ?";
        _costText.text = "?";
    }

    private void OnClickRepair()
    {
        if (!_repairSlot.IsSetted) return;

        // TODO: 수리 로직 구현
    }
}
