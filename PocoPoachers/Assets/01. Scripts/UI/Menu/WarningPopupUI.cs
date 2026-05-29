using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  WarningPopup [WarningPopupUI] — 기본 비활성화
//  ├── Popup_Dimmer    [Image]
//  └── Popup_Panel
//      ├── Txt_Title   [TextMeshProUGUI]
//      ├── Txt_Message [TextMeshProUGUI]
//      ├── Btn_Confirm [Button]
//      └── Btn_Cancel  [Button]
// ────────────────────────────────────────────────────────────────────────

public class WarningPopupUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _txtTitle;
    [SerializeField] private TextMeshProUGUI _txtMessage;
    [SerializeField] private Button          _btnConfirm;
    [SerializeField] private Button          _btnCancel;

    public event Action OnConfirmed;
    public event Action OnCancelled;

    private void Awake()
    {
        UIManager.GetInstance().Register(UIType.WarningPopup, gameObject);

        _btnConfirm.onClick.AddListener(OnClickConfirm);
        _btnCancel .onClick.AddListener(OnClickCancel);

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        var ui = UIManager.GetInstance();
        if (ui == null) return;
        ui.Unregister(UIType.WarningPopup);
    }

    public void Show(string title, string message)
    {
        if (_txtTitle != null)   _txtTitle.text   = title;
        if (_txtMessage != null) _txtMessage.text = message;
        UIManager.GetInstance().Show(UIType.WarningPopup);
    }

    private void OnClickConfirm()
    {
        UIManager.GetInstance().Hide(UIType.WarningPopup);
        OnConfirmed?.Invoke();
    }

    private void OnClickCancel()
    {
        UIManager.GetInstance().Hide(UIType.WarningPopup);
        OnCancelled?.Invoke();
    }
}
