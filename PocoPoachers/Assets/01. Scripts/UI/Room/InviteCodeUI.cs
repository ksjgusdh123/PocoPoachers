using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  InviteCodeUI
//  ├─ BtnInvite       (Button)             — 초대 버튼 (호스트만 표시)
//  └─ Popup           (GameObject)         — 코드 팝업
//      ├─ TxtCode     (TextMeshProUGUI)    — 초대 코드
//      ├─ BtnCopy     (Button)             — 클립보드 복사
//      └─ BtnClose    (Button)             — 닫기
// ────────────────────────────────────────────────────────────────────────

public class InviteCodeUI : MonoBehaviour
{
    [SerializeField] Button          _btnInvite;
    [SerializeField] GameObject      _popup;
    [SerializeField] TextMeshProUGUI _txtCode;
    [SerializeField] Button          _btnCopy;
    [SerializeField] Button          _btnClose;

    void Awake()
    {
        _btnInvite.onClick.AddListener(OnClickInvite);
        _btnCopy.onClick.AddListener(OnClickCopy);
        _btnClose.onClick.AddListener(() => _popup.SetActive(false));
        _popup.SetActive(false);

        _btnInvite.gameObject.SetActive(RoomManager.IsHost);
    }

    void OnClickInvite()
    {
        string code = RoomManager.Instance.SessionCode;
        if (string.IsNullOrEmpty(code)) return;
        _txtCode.text = code;
        _popup.SetActive(true);
    }

    void OnClickCopy()
    {
        GUIUtility.systemCopyBuffer = RoomManager.Instance.SessionCode;
    }
}
