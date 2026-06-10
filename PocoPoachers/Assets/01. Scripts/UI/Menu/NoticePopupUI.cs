using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  NoticePopup [NoticePopupUI]
//  ├── Popup_Dimmer    [Image]
//  └── Popup_Panel
//      ├── Txt_Title   [TextMeshProUGUI]
//      ├── Txt_Message [TextMeshProUGUI]
//      └── Btn_Ok      [Button]
// ────────────────────────────────────────────────────────────────────────

public class NoticePopupUI : UIBase
{
    [SerializeField] private TextMeshProUGUI _txtTitle;
    [SerializeField] private TextMeshProUGUI _txtMessage;
    [SerializeField] private Button          _btnOk;

    public event Action OnOk;

    protected override UIType UiType => UIType.NoticePopup;

    protected override void Awake()
    {
        base.Awake();

        _btnOk.onClick.AddListener(() => OnOk?.Invoke());
    }

    protected override void RegisterSelf()   => UIManager.GetInstance().RegisterNoticePopup(this);
    protected override void UnregisterSelf() => UIManager.GetInstance().UnregisterNoticePopup();

    public void SetContent(string title, string message)
    {
        if (_txtTitle   != null) _txtTitle.text   = title;
        if (_txtMessage != null) _txtMessage.text = message;
    }
}
