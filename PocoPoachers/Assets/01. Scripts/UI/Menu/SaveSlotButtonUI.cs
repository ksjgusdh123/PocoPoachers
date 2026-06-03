using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaveSlotButtonUI : MonoBehaviour
{
    public static event Action<int> OnSlotSelected;

    [SerializeField] private TextMeshProUGUI _txtInfo;

    private Button _btn;
    private int _slotIndex;

    private void Awake()
    {
        _btn = GetComponent<Button>();
        _btn.onClick.AddListener(() => OnSlotSelected?.Invoke(_slotIndex));
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
    }
}
