using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  LobbyUI
//  ├─ BtnNewGame  (Button)         — 새 게임
//  ├─ BtnLoad     (Button)         — 불러오기
//  ├─ BtnCoOp     (Button)         — 협동플레이
//  ├─ BtnQuit     (Button)         — 나가기
//  └─ CoOpUI      (SessionCodeUI)  — 협동플레이 패널 오브젝트
// ────────────────────────────────────────────────────────────────────────

public class LobbyUI : MonoBehaviour
{
    [SerializeField] Button         _btnNewGame;
    [SerializeField] Button         _btnLoad;
    [SerializeField] Button         _btnCoOp;
    [SerializeField] Button         _btnQuit;
    [SerializeField] SessionCodeUI  _coOpUI;

    void Awake()
    {
        _btnNewGame.onClick.AddListener(OnClickNewGame);
        _btnLoad   .onClick.AddListener(OnClickLoad);
        _btnCoOp   .onClick.AddListener(OnClickCoOp);
        _btnQuit   .onClick.AddListener(OnClickQuit);

        RoomManager.Instance.OnRoomJoinFailed += OnRoomJoinFailed;
        _coOpUI.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnRoomJoinFailed -= OnRoomJoinFailed;
    }

    void OnClickNewGame()
    {
        SetButtonsInteractable(false);
        var nm = NetworkManager.Instance;
        if (nm != null && nm.Session != null && nm.Session.IsConnected)
            RoomManager.Instance.StartAsHost();
        else
            RoomManager.Instance.StartLocalHost();
    }

    void OnClickLoad()
    {
        // TODO: 세이브 데이터 로드 후 StartAsHost
    }

    void OnClickCoOp() => _coOpUI.gameObject.SetActive(true);

    void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnRoomJoinFailed(string _) => SetButtonsInteractable(true);

    void SetButtonsInteractable(bool interactable)
    {
        _btnNewGame.interactable = interactable;
        _btnLoad   .interactable = interactable;
        _btnCoOp   .interactable = interactable;
        _btnQuit   .interactable = interactable;
    }
}
