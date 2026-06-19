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
    private bool _hasSave;

    private void Awake()
    {
        _btn.onClick.AddListener(() => OnSlotSelected?.Invoke(_slotIndex));
        _btnDelete?.onClick.AddListener(() =>
        {
            SaveManager.GetInstance().DeleteSlot(_slotIndex);
            OnSlotDeleted?.Invoke(_slotIndex);
        });
    }

    private void OnEnable()
    {
        LocalizationManager.GetInstance().OnLanguageChanged += RefreshInfoText;
    }

    private void OnDisable()
    {
        var manager = LocalizationManager.GetInstance();
        if (manager == null) return;
        manager.OnLanguageChanged -= RefreshInfoText;
    }

    public void Init(int slotIndex)
    {
        _slotIndex = slotIndex;

        var sm = SaveManager.GetInstance();
        _hasSave = sm.HasSave(slotIndex);

        RefreshInfoText();

        if (_btn != null)
            _btn.interactable = _hasSave;

        if (_btnDelete != null)
            _btnDelete.interactable = _hasSave;
    }

    private void RefreshInfoText()
    {
        if (_txtInfo == null) return;
        var sm = SaveManager.GetInstance();
        _txtInfo.text = _hasSave ? sm.GetLastSavedAt(_slotIndex) : LocalizationManager.GetInstance().GetString("save.empty_slot");
    }
}
