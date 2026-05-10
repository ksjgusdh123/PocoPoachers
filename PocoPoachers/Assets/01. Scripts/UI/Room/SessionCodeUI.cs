using Mono.Cecil.Cil;
using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  SessionCodeUI (Canvas > Panel)
//  ├─ SelectView          (방 만들기 / 코드로 참가 선택 화면)
//  │   ├─ BtnHost         (Button)
//  │   └─ BtnJoin         (Button)
//  ├─ HostView            (코드 표시 + 대기 화면)
//  │   ├─ TxtCode         (TextMeshProUGUI)  — 생성된 6자리 코드
//  │   ├─ BtnCopy         (Button)           — 클립보드 복사
//  │   ├─ TxtStatus       (TextMeshProUGUI)  — "친구를 기다리는 중..."
//  │   └─ BtnCancel       (Button)
//  ├─ JoinView            (코드 입력 화면)
//  │   ├─ InputCode       (TMP_InputField)   — 6자리 입력
//  │   ├─ BtnConfirm      (Button)           — 참가하기
//  │   └─ BtnBack         (Button)
//  └─ StatusView          (연결 중 / 결과 화면)
//      ├─ TxtStatus       (TextMeshProUGUI)
//      └─ BtnClose        (Button)           — 실패 시만 활성화
// ────────────────────────────────────────────────────────────────────────

public class SessionCodeUI : MonoBehaviour
{
    [Header("Views")]
    [SerializeField] GameObject _selectView;
    [SerializeField] GameObject _hostView;
    [SerializeField] GameObject _joinView;
    [SerializeField] GameObject _statusView;

    [Header("Select")]
    [SerializeField] Button _btnHost;
    [SerializeField] Button _btnJoin;

    [Header("Host")]
    [SerializeField] TextMeshProUGUI _txtCode;
    [SerializeField] Button          _btnCopy;
    [SerializeField] TextMeshProUGUI _txtHostStatus;
    [SerializeField] Button          _btnHostCancel;

    [Header("Join")]
    [SerializeField] TMP_InputField  _inputCode;
    [SerializeField] Button          _btnConfirm;
    [SerializeField] Button          _btnBack;

    [Header("Status")]
    [SerializeField] TextMeshProUGUI _txtStatus;
    [SerializeField] Button          _btnClose;

    public static SessionCodeUI Instance { get; private set; }

    string _sessionCode;

    void Awake()
    {
        Instance = this;
        _btnHost.onClick.AddListener(OnClickHost);
        _btnJoin.onClick.AddListener(OnClickJoin);
        _btnCopy.onClick.AddListener(OnClickCopy);
        _btnHostCancel.onClick.AddListener(OnClickCancel);
        _btnConfirm.onClick.AddListener(OnClickConfirm);
        _btnBack.onClick.AddListener(OnClickBack);
        _btnClose.onClick.AddListener(OnClickCancel);

        _inputCode.characterLimit = 6;
        _inputCode.onValueChanged.AddListener(v =>
            _btnConfirm.interactable = v.Length == 6);
        _btnConfirm.interactable = false;

        RoomManager.Instance.OnGameStarted   += HandleGameStarted;
        RoomManager.Instance.OnRoomJoinFailed += HandleFailed;
        ShowView(_selectView);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (RoomManager.Instance == null) return;
        RoomManager.Instance.OnGameStarted   -= HandleGameStarted;
        RoomManager.Instance.OnRoomJoinFailed -= HandleFailed;
    }

    void OnClickHost()
    {
        var nm = NetworkManager.Instance;
        if (nm != null && nm.Session != null && nm.Session.IsConnected)
        {
            _txtHostStatus.text = "방 생성 중...";
            ShowView(_hostView);
            RoomManager.Instance.StartAsHost();
        }
        else
        {
            // TEMP: 서버 미연결 시 로컬 즉시 시작
            RoomManager.Instance.StartLocalHost();
            gameObject.SetActive(false);
        }
    }

    void OnClickJoin()
    {
        _inputCode.text = "";
        ShowView(_joinView);
    }

    void OnClickCopy()
    {
        GUIUtility.systemCopyBuffer = _sessionCode;
        StartCoroutine(FlashCopyFeedback());
    }

    IEnumerator FlashCopyFeedback()
    {
        string original = _txtHostStatus.text;
        _txtHostStatus.text = "클립보드에 복사됨!";
        yield return new WaitForSeconds(1.5f);
        if (_txtHostStatus != null) _txtHostStatus.text = original;
    }

    void OnClickConfirm()
    {
        _sessionCode = _inputCode.text.ToUpper();
        SetStatus("연결 중...", false);
        RoomManager.Instance.StartAsGuest(_sessionCode);
    }

    void OnClickBack() => ShowView(_selectView);


    void OnClickCancel()
    {
        RoomManager.Instance.LeaveRoom();
        ShowView(_selectView);
    }

    public void HandleCreateRoom(string code, bool success)
    {
        if (success)
        {
            _sessionCode  = code;
            _txtCode.text = _sessionCode;
            // OnGameStarted 이후 HandleGameStarted에서 상태 문자 갱신
        }
        else
        {
            _txtStatus.text = "방 생성에 실패했습니다.";
            ShowView(_statusView);
        }
    }

    public void HandleJoinRoom(bool success)
    {
        if (success)
        {
            _txtStatus.text = "방장 정보를 찾았습니다. 연결 중...";
        }
        else
        {
            _txtStatus.text = "방이 존재하지 않거나 입장이 불가능합니다.";
            ShowView(_statusView);
        }
    }

    void HandleGameStarted()
    {
        if (RoomManager.IsHost)
        {
            // 게임 시작 - HostView에서 코드를 공유하고 닫기 버튼으로 계속
            _txtHostStatus.text = "게임 시작! 코드를 친구에게 공유하세요.";
        }
        else
        {
            SetStatus("연결 성공!", false);
            // TODO: 씬 전환 또는 게임 시작 처리
        }
    }

    void HandleFailed(string reason)
    {
        SetStatus($"연결 실패: {reason}", true);
    }

    void SetStatus(string msg, bool showClose)
    {
        _txtStatus.text       = msg;
        _btnClose.gameObject.SetActive(showClose);
        ShowView(_statusView);
    }

    void ShowView(GameObject target)
    {
        _selectView.SetActive(target == _selectView);
        _hostView  .SetActive(target == _hostView);
        _joinView  .SetActive(target == _joinView);
        _statusView.SetActive(target == _statusView);
    }
}
