using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  InviteCodeUI
//  ├─ BtnInvite       (Button)             — 초대 버튼 (호스트만 표시)
//  ├─ TxtStatus       (TextMeshProUGUI)    — 상태 안내 텍스트 (선택)
//  └─ Popup           (GameObject)         — 코드 팝업
//      ├─ TxtCode     (TextMeshProUGUI)    — 초대 코드
//      ├─ BtnCopy     (Button)             — 클립보드 복사
//      └─ BtnClose    (Button)             — 닫기
// ────────────────────────────────────────────────────────────────────────

public class InviteCodeUI : MonoBehaviour
{
    [SerializeField] Button          _btnInvite;
    [SerializeField] TextMeshProUGUI _txtStatus;
    [SerializeField] GameObject      _popup;
    [SerializeField] TextMeshProUGUI _txtCode;
    [SerializeField] Button          _btnCopy;
    [SerializeField] Button          _btnClose;

    const float LOGIN_TIMEOUT = 5f;

    void Awake()
    {
        _btnInvite.onClick.AddListener(OnClickInvite);
        _btnCopy.onClick.AddListener(OnClickCopy);
        _btnClose.onClick.AddListener(() =>
        {
            _popup.SetActive(false);
            _btnInvite.gameObject.SetActive(true);
        });
        _popup.SetActive(false);

        _btnInvite.gameObject.SetActive(RoomManager.IsHost);
        if (_txtStatus != null) _txtStatus.gameObject.SetActive(false);

        RoomManager.Instance.OnSessionCodeReceived += OnCodeReceived;
        RoomManager.Instance.OnRoomJoinFailed      += OnRoomJoinFailed;
    }

    void OnDestroy()
    {
        if (RoomManager.Instance == null) return;
        RoomManager.Instance.OnSessionCodeReceived -= OnCodeReceived;
        RoomManager.Instance.OnRoomJoinFailed      -= OnRoomJoinFailed;
    }

    void OnClickInvite()
    {
        SetStatus("");

        string code = RoomManager.Instance.SessionCode;
        if (!string.IsNullOrEmpty(code))
        {
            ShowPopup(code);
            return;
        }

        _btnInvite.interactable = false;

        var nm = NetworkManager.Instance;
        if (nm != null && nm.Session != null && nm.Session.IsConnected)
        {
            RoomManager.Instance.StartAsHost();
        }
        else
        {
            SetStatus("서버에 연결 중...");
            NetworkManager.Instance.OnConnected += OnServerConnected;
            NetworkManager.Instance.Reconnect();
        }
    }

    void OnServerConnected()
    {
        NetworkManager.Instance.OnConnected -= OnServerConnected;
        StartCoroutine(CoWaitLoginThenHost());
    }

    IEnumerator CoWaitLoginThenHost()
    {
        float elapsed = 0f;
        while (!NetworkManager.Instance.IsLoggedIn && elapsed < LOGIN_TIMEOUT)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!NetworkManager.Instance.IsLoggedIn)
        {
            SetStatus("서버에 연결할 수 없습니다.");
            _btnInvite.interactable = true;
            yield break;
        }

        SetStatus("");
        RoomManager.Instance.StartAsHost();
    }

    void OnCodeReceived(string code)
    {
        _btnInvite.interactable = true;
        ShowPopup(code);
    }

    void OnRoomJoinFailed(string _)
    {
        _btnInvite.interactable = true;
        SetStatus("방 생성에 실패했습니다.");
    }

    void ShowPopup(string code)
    {
        _txtCode.text = code;
        _popup.SetActive(true);
        _btnInvite.gameObject.SetActive(false);
    }

    void OnClickCopy()
    {
        GUIUtility.systemCopyBuffer = RoomManager.Instance.SessionCode;
    }

    void SetStatus(string msg)
    {
        if (_txtStatus == null) return;
        bool hasMsg = !string.IsNullOrEmpty(msg);
        _txtStatus.gameObject.SetActive(hasMsg);
        if (hasMsg) _txtStatus.text = msg;
    }
}
