using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  SessionCodeUI (Panel — 협동플레이 패널)
//  ├─ InputCode   (TMP_InputField)     — 6자리 코드
//  ├─ BtnJoin     (Button)             — 참가하기
//  ├─ BtnBack     (Button)             — 뒤로
//  └─ StatusView  (GameObject)         — 연결 중 / 실패
//      ├─ TxtStatus   (TextMeshProUGUI)
//      └─ BtnClose    (Button)
// ────────────────────────────────────────────────────────────────────────

public class SessionCodeUI : MonoBehaviour
{
    [SerializeField] TMP_InputField  _inputCode;
    [SerializeField] Button          _btnJoin;
    [SerializeField] Button          _btnBack;
    [SerializeField] GameObject      _statusView;
    [SerializeField] TextMeshProUGUI _txtStatus;
    [SerializeField] Button          _btnClose;

    public static SessionCodeUI Instance { get; private set; }

    void Awake()
    {
        Instance = this;
        _btnJoin.onClick.AddListener(OnClickJoin);
        _btnBack.onClick.AddListener(() => gameObject.SetActive(false));
        _btnClose.onClick.AddListener(OnClickClose);

        _inputCode.characterLimit = 6;
        _inputCode.onValueChanged.AddListener(v => _btnJoin.interactable = v.Length == 6);
        _btnJoin.interactable = false;

        RoomManager.Instance.OnRoomJoinFailed += HandleFailed;
        _statusView.SetActive(false);
    }

    void OnEnable()
    {
        _inputCode.text = "";
        _btnJoin.interactable = false;
        _statusView.SetActive(false);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnRoomJoinFailed -= HandleFailed;
    }

    void OnClickJoin()
    {
        ShowStatus("연결 중...", false);
        RoomManager.Instance.StartAsGuest(_inputCode.text.ToUpper());
    }

    void OnClickClose()
    {
        RoomManager.Instance.LeaveRoom();
        _statusView.SetActive(false);
    }

    public void HandleJoinRoom(bool success)
    {
        if (!success)
            ShowStatus("방이 존재하지 않거나 입장이 불가능합니다.", true);
    }

    void HandleFailed(string reason) => ShowStatus($"연결 실패: {reason}", true);

    void ShowStatus(string msg, bool showClose)
    {
        _txtStatus.text = msg;
        _btnClose.gameObject.SetActive(showClose);
        _statusView.SetActive(true);
    }
}
