using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotButtonUI : MonoBehaviour
{
    public static event Action<int> OnSlotSelected;
    public static event Action<int> OnSlotDeleted;

    [SerializeField] private Button _btn;
    [SerializeField] private TextMeshProUGUI _txtInfo;
    [SerializeField] private Button _btnDelete;

    private int _slotIndex;

    private void Awake()
    {
        _btn.onClick.AddListener(() => OnSlotSelected?.Invoke(_slotIndex));
        _btnDelete?.onClick.AddListener(() =>
        {
            SaveManager.GetInstance().DeleteSlot(_slotIndex);
            OnSlotDeleted?.Invoke(_slotIndex);
        });
    }

    public void Init(int slotIndex)
    {
        _slotIndex = slotIndex;

        var sm = SaveManager.GetInstance();
        bool hasSave = sm.HasSave(slotIndex);

        if (_txtInfo != null)
            _txtInfo.text = hasSave ? sm.GetLastSavedAt(slotIndex) : "빈 슬롯";

        if (_btn != null)
            _btn.interactable = hasSave;

        if (_btnDelete != null)
            _btnDelete.interactable = hasSave;
    }
}
