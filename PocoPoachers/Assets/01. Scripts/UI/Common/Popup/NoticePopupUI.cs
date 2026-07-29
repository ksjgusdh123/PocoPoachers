using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  NoticePopupUI [UIPopupFrame]
//  └── Content
//      ├── Txt_Message [TextMeshProUGUI]
//      └── Btn_Ok      [Button]
// ────────────────────────────────────────────────────────────────────────

public class NoticePopupUI : PopupUIBase
{
    [SerializeField] private TextMeshProUGUI _txtMessage;
    [SerializeField] private Button          _btnOk;

    public event Action OnOk;

    protected override UIType UiType => UIType.NoticePopup;

    protected override TextMeshProUGUI MessageText => _txtMessage;

    protected override void Awake()
    {
        base.Awake();

        _btnOk.onClick.AddListener(() => OnOk?.Invoke());
    }

    protected override void RegisterToManager() => UIManager.GetInstance().RegisterNoticePopup(this);
    protected override void UnregisterSelf()    => UIManager.GetInstance().UnregisterNoticePopup();
}
