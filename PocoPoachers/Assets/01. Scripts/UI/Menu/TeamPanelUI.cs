using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  TeamPanel [TeamPanelUI]
//  ├── PlayerEntry_0          [GameObject]
//  ├── PlayerEntry_1          [GameObject]
//  ├── PlayerEntry_2          [GameObject]
//  ├── PlayerEntry_3          [GameObject]
//  ├── Btn_Invite             [Button]           — 호스트만 표시
//  ├── Txt_Status             [TextMeshProUGUI]  (선택)
//  ├── Txt_Code               [TextMeshProUGUI]  — 코드 생성 후 표시
//  └── Btn_Copy               [Button]           — 코드 생성 후 표시
// ────────────────────────────────────────────────────────────────────────

public class TeamPanelUI : MonoBehaviour
{
    private const float LOGIN_TIMEOUT = 5f;

    [Header("Team Slots")]
    [SerializeField] private GameObject[] _playerSlots = new GameObject[4];

    [Header("Invite")]
    [SerializeField] private Button          _btnInvite;
    [SerializeField] private TextMeshProUGUI _txtStatus;
    [SerializeField] private TextMeshProUGUI _txtCode;
    [SerializeField] private Button          _btnCopy;
    [SerializeField] private NoticePopupUI   _noticePopup;

    private void Awake()
    {
        _btnInvite.onClick.AddListener(OnClickInvite);
        _btnCopy  .onClick.AddListener(OnClickCopy);

        _btnInvite.gameObject.SetActive(RoomManager.IsHost);
        if (_txtStatus != null) _txtStatus.gameObject.SetActive(false);
        _txtCode.gameObject.SetActive(false);
        _btnCopy.gameObject.SetActive(false);

        RoomManager.Instance.OnSessionCodeReceived += OnCodeReceived;
        RoomManager.Instance.OnRoomJoinFailed      += OnRoomJoinFailed;
    }

    private void OnDestroy()
    {
        if (RoomManager.Instance == null) return;
        RoomManager.Instance.OnSessionCodeReceived -= OnCodeReceived;
        RoomManager.Instance.OnRoomJoinFailed      -= OnRoomJoinFailed;
    }

    // ── Team List ──────────────────────────────────────────────────────

    public void RefreshTeamList(int activeMemberCount)
    {
        for (int i = 0; i < _playerSlots.Length; i++)
            _playerSlots[i].SetActive(i < activeMemberCount);
    }

    // ── Invite ─────────────────────────────────────────────────────────

    private void OnClickInvite()
    {
        SetStatus("");
        string code = RoomManager.Instance.SessionCode;
        if (!string.IsNullOrEmpty(code)) { ShowCode(code); return; }

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

    private void OnServerConnected()
    {
        NetworkManager.Instance.OnConnected -= OnServerConnected;
        StartCoroutine(CoWaitLoginThenHost());
    }

    private IEnumerator CoWaitLoginThenHost()
    {
        float elapsed = 0f;
        while (!NetworkManager.Instance.IsLoggedIn && elapsed < LOGIN_TIMEOUT)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!NetworkManager.Instance.IsLoggedIn)
        {
            SetStatus("");
            _noticePopup?.Show("연결 실패", "서버에 연결할 수 없습니다.");
            _btnInvite.interactable = true;
            yield break;
        }

        SetStatus("");
        RoomManager.Instance.StartAsHost();
    }

    private void OnCodeReceived(string code)
    {
        _btnInvite.interactable = true;
        ShowCode(code);
    }

    private void OnRoomJoinFailed(string _)
    {
        _btnInvite.interactable = true;
        SetStatus("");
        _noticePopup?.Show("초대 실패", "방 생성에 실패했습니다.");
    }

    private void ShowCode(string code)
    {
        _txtCode.text = code;
        _txtCode.gameObject.SetActive(true);
        _btnCopy.gameObject.SetActive(true);
    }

    private void OnClickCopy() => GUIUtility.systemCopyBuffer = RoomManager.Instance.SessionCode;

    private void SetStatus(string msg)
    {
        if (_txtStatus == null) return;
        bool hasMsg = !string.IsNullOrEmpty(msg);
        _txtStatus.gameObject.SetActive(hasMsg);
        if (hasMsg) _txtStatus.text = msg;
    }
}
