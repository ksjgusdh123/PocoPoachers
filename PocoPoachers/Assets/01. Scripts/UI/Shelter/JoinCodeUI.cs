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

public class JoinCodeUI : UIBase
{
    private const float CONNECT_TIMEOUT = 5f;

    [SerializeField] TMP_InputField _inputCode;
    [SerializeField] Button         _btnJoin;
    [SerializeField] Button         _btnBack;

    public static JoinCodeUI Instance { get; private set; }

    protected override UIType UiType => UIType.JoinCode;

    protected override void Awake()
    {
        base.Awake();

        Instance = this;

        _btnJoin.onClick.AddListener(OnClickJoin);
        _btnBack.onClick.AddListener(Hide);

        _inputCode.characterLimit = 6;
        _inputCode.onValueChanged.AddListener(v => _btnJoin.interactable = v.Length == 6);
        _btnJoin.interactable = false;

        RoomManager.Instance.OnRoomJoinFailed += HandleFailed;
    }

    void OnEnable()
    {
        _inputCode.text = "";
        _btnJoin.interactable = false;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (Instance == this) Instance = null;

        if (RoomManager.Instance != null)
            RoomManager.Instance.OnRoomJoinFailed -= HandleFailed;
    }

    void OnClickJoin()
    {
        var nm = NetworkManager.Instance;
        if (nm == null)
        {
            UIManager.GetInstance().ShowNotice(
                LocalizationManager.GetInstance().GetString("network.not_connected_title"),
                LocalizationManager.GetInstance().GetString("network.not_connected_message"));
            return;
        }

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
            UIManager.GetInstance().ShowNotice(
                LocalizationManager.GetInstance().GetString("network.connect_failed_title"),
                LocalizationManager.GetInstance().GetString("network.connect_failed_message"));
            _btnJoin.interactable = _inputCode.text.Length == 6;
            yield break;
        }

        RoomManager.Instance.StartAsGuest(_inputCode.text.ToUpper());
    }

    public void HandleJoinRoom(bool success)
    {
        if (!success)
            UIManager.GetInstance().ShowNotice(
                LocalizationManager.GetInstance().GetString("network.join_failed_title"),
                LocalizationManager.GetInstance().GetString("network.join_failed_message"));
    }

    void HandleFailed(string reason)
    {
        _btnJoin.interactable = _inputCode.text.Length == 6;
        UIManager.GetInstance().ShowNotice(
            LocalizationManager.GetInstance().GetString("network.connect_failed_title"), reason);
    }
}
