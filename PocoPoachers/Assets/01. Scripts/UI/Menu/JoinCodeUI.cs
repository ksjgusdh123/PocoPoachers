using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  JoinCodeUI (Panel — 협동플레이 패널)
//  ├─ InputCode   (TMP_InputField)  — 6자리 코드
//  ├─ BtnJoin     (Button)          — 참가하기
//  └─ BtnBack     (Button)          — 뒤로
// ────────────────────────────────────────────────────────────────────────

public class JoinCodeUI : MonoBehaviour
{
    private const float CONNECT_TIMEOUT = 5f;

    [SerializeField] TMP_InputField _inputCode;
    [SerializeField] Button         _btnJoin;
    [SerializeField] Button         _btnBack;
    [SerializeField] NoticePopupUI  _noticePopup;

    public static JoinCodeUI Instance { get; private set; }

    void Awake()
    {
        Instance = this;

        UIManager.GetInstance().Register(UIType.JoinCode, gameObject);

        _btnJoin.onClick.AddListener(OnClickJoin);
        _btnBack.onClick.AddListener(() => UIManager.GetInstance().Hide(UIType.JoinCode));

        _inputCode.characterLimit = 6;
        _inputCode.onValueChanged.AddListener(v => _btnJoin.interactable = v.Length == 6);
        _btnJoin.interactable = false;

        RoomManager.Instance.OnRoomJoinFailed += HandleFailed;

        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        _inputCode.text = "";
        _btnJoin.interactable = false;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;

        var ui = UIManager.GetInstance();
        if (ui != null) ui.Unregister(UIType.JoinCode);

        if (RoomManager.Instance != null)
            RoomManager.Instance.OnRoomJoinFailed -= HandleFailed;
    }

    public void Show() => UIManager.GetInstance().Show(UIType.JoinCode);

    void OnClickJoin()
    {
        var nm = NetworkManager.Instance;
        if (nm == null) { _noticePopup?.Show("연결 오류", "서버에 연결되어 있지 않습니다."); return; }

        _btnJoin.interactable = false;

        if (nm.IsLoggedIn)
        {
            RoomManager.Instance.StartAsGuest(_inputCode.text.ToUpper());
        }
        else
        {
            if (nm.Session == null || !nm.Session.IsConnected)
                nm.Reconnect();
            StartCoroutine(CoWaitLoginThenJoin());
        }
    }

    IEnumerator CoWaitLoginThenJoin()
    {
        float elapsed = 0f;
        while (!NetworkManager.Instance.IsLoggedIn && elapsed < CONNECT_TIMEOUT)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!NetworkManager.Instance.IsLoggedIn)
        {
            _noticePopup?.Show("연결 실패", "서버에 연결할 수 없습니다.");
            _btnJoin.interactable = _inputCode.text.Length == 6;
            yield break;
        }

        RoomManager.Instance.StartAsGuest(_inputCode.text.ToUpper());
    }

    public void HandleJoinRoom(bool success)
    {
        if (!success)
            _noticePopup?.Show("참가 실패", "방이 존재하지 않거나 입장이 불가능합니다.");
    }

    void HandleFailed(string reason) => _noticePopup?.Show("연결 실패", reason);
}
