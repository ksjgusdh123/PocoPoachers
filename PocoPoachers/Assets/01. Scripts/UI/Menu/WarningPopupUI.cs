using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  WarningPopup [WarningPopupUI]
//  ├── Popup_Dimmer    [Image]
//  └── Popup_Panel
//      ├── Txt_Title   [TextMeshProUGUI]
//      ├── Txt_Message [TextMeshProUGUI]
//      ├── Btn_Confirm [Button]
//      └── Btn_Cancel  [Button]
// ────────────────────────────────────────────────────────────────────────

public class WarningPopupUI : UIBase
{
    [SerializeField] private TextMeshProUGUI _txtTitle;
    [SerializeField] private TextMeshProUGUI _txtMessage;
    [SerializeField] private Button          _btnConfirm;
    [SerializeField] private Button          _btnCancel;

    public event Action OnConfirmed;
    public event Action OnCancelled;

    protected override UIType UiType => UIType.WarningPopup;

    protected override void Awake()
    {
        base.Awake();

        _btnConfirm.onClick.AddListener(() => OnConfirmed?.Invoke());
        _btnCancel .onClick.AddListener(() => OnCancelled?.Invoke());
    }

    protected override void RegisterSelf()   => UIManager.GetInstance().RegisterWarningPopup(this);
    protected override void UnregisterSelf() => UIManager.GetInstance().UnregisterWarningPopup();

    public void SetContent(string title, string message)
    {
        if (_txtTitle   != null) _txtTitle.text   = title;
        if (_txtMessage != null) _txtMessage.text = message;
    }
}
