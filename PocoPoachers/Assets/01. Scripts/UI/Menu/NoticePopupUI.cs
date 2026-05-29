using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  NoticePopup [NoticePopupUI] — 기본 비활성화
//  ├── Popup_Dimmer    [Image]
//  └── Popup_Panel
//      ├── Txt_Title   [TextMeshProUGUI]
//      ├── Txt_Message [TextMeshProUGUI]
//      └── Btn_Ok      [Button]
// ────────────────────────────────────────────────────────────────────────

public class NoticePopupUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _txtTitle;
    [SerializeField] private TextMeshProUGUI _txtMessage;
    [SerializeField] private Button          _btnOk;

    private void Awake()
    {
        UIManager.GetInstance().Register(UIType.NoticePopup, gameObject);
        _btnOk.onClick.AddListener(OnClickOk);
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        var ui = UIManager.GetInstance();
        if (ui == null) return;
        ui.Unregister(UIType.NoticePopup);
    }

    public void Show(string title, string message)
    {
        if (_txtTitle != null)   _txtTitle.text   = title;
        if (_txtMessage != null) _txtMessage.text = message;
        UIManager.GetInstance().Show(UIType.NoticePopup);
    }

    private void OnClickOk() => UIManager.GetInstance().Hide(UIType.NoticePopup);
}
