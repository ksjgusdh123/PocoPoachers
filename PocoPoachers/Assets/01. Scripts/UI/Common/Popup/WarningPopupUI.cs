using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  WarningPopupUI [UIPopupFrame]
//  └── Content
//      ├── Txt_Message [TextMeshProUGUI]
//      ├── Btn_Confirm [Button]
//      └── Btn_Cancel  [Button]
// ────────────────────────────────────────────────────────────────────────

public class WarningPopupUI : PopupUIBase
{
    [SerializeField] private TextMeshProUGUI _txtMessage;
    [SerializeField] private Button          _btnConfirm;
    [SerializeField] private Button          _btnCancel;

    public event Action OnConfirmed;
    public event Action OnCancelled;

    protected override UIType UiType => UIType.WarningPopup;

    protected override TextMeshProUGUI MessageText => _txtMessage;

    protected override void Awake()
    {
        base.Awake();

        _btnConfirm.onClick.AddListener(() => OnConfirmed?.Invoke());
        _btnCancel .onClick.AddListener(() => OnCancelled?.Invoke());
    }

    protected override void RegisterSelf()   => UIManager.GetInstance().RegisterWarningPopup(this);
    protected override void UnregisterSelf() => UIManager.GetInstance().UnregisterWarningPopup();
}
