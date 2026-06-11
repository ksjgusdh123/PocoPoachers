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

public class WarningPopupUI : PopupUIBase
{
    [SerializeField] private TextMeshProUGUI _txtTitle;
    [SerializeField] private TextMeshProUGUI _txtMessage;
    [SerializeField] private Button          _btnConfirm;
    [SerializeField] private Button          _btnCancel;

    public event Action OnConfirmed;
    public event Action OnCancelled;

    protected override UIType UiType => UIType.WarningPopup;

    protected override TextMeshProUGUI TitleText   => _txtTitle;
    protected override TextMeshProUGUI MessageText => _txtMessage;
    protected override Button[]        Buttons     => new[] { _btnConfirm, _btnCancel };

    protected override void Awake()
    {
        base.Awake();

        _btnConfirm.onClick.AddListener(() => OnConfirmed?.Invoke());
        _btnCancel .onClick.AddListener(() => OnCancelled?.Invoke());
    }

    protected override void RegisterSelf()   => UIManager.GetInstance().RegisterWarningPopup(this);
    protected override void UnregisterSelf() => UIManager.GetInstance().UnregisterWarningPopup();
}
