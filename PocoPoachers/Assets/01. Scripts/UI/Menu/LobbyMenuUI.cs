using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  LobbyMenuUI
//  ├─ BtnNewGame  (Button)
//  ├─ BtnLoad     (Button)
//  ├─ BtnCoOp     (Button)
//  ├─ BtnQuit     (Button)
//  └─ CoOpUI      (JoinCodeUI)
// ────────────────────────────────────────────────────────────────────────

public class LobbyMenuUI : MonoBehaviour
{
    private const float CONNECT_TIMEOUT = 5f;

    [SerializeField] Button     _btnNewGame;
    [SerializeField] Button     _btnLoad;
    [SerializeField] Button     _btnCoOp;
    [SerializeField] Button     _btnQuit;
    [SerializeField] JoinCodeUI _coOpUI;

    void Awake()
    {
        RoomManager.Instance.OnGameStarted    += OnGameStarted;
        RoomManager.Instance.OnRoomJoinFailed += OnRoomJoinFailed;

        _btnNewGame.onClick.AddListener(OnClickNewGame);
        _btnLoad   .onClick.AddListener(OnClickLoad);
        _btnCoOp   .onClick.AddListener(OnClickCoOp);
        _btnQuit   .onClick.AddListener(OnClickQuit);

        _coOpUI.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnGameStarted    -= OnGameStarted;
            RoomManager.Instance.OnRoomJoinFailed -= OnRoomJoinFailed;
        }
    }

    void OnGameStarted() => SceneLoader.Instance.LoadGameScene();

    void OnClickNewGame()
    {
        SetButtonsInteractable(false);
        StartCoroutine(CoConnectThen(
            onSuccess: () => RoomManager.Instance.StartAsHost(),
            onFail:    () => UIManager.GetInstance().ShowWarning(
                "연결 실패",
                "서버에 연결할 수 없습니다.\n로컬 플레이로 진행하시겠습니까?",
                onConfirm: () => RoomManager.Instance.StartLocalHost(),
                onCancel:  () => SetButtonsInteractable(true)
            )
        ));
    }

    void OnClickLoad()
    {
        // TODO: 세이브 데이터 로드 후 StartAsHost
    }

    void OnClickCoOp()
    {
        SetButtonsInteractable(false);
        StartCoroutine(CoConnectThen(
            onSuccess: () => { SetButtonsInteractable(true); _coOpUI.Show(); },
            onFail:    () =>
            {
                SetButtonsInteractable(true);
                UIManager.GetInstance().ShowNotice("연결 실패", "서버에 연결할 수 없습니다.\n협동 플레이는 서버 연결이 필요합니다.");
            }
        ));
    }

    void OnClickQuit()
    {
        NetworkManager.Instance?.LeaveGame();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnRoomJoinFailed(string _) => SetButtonsInteractable(true);

    IEnumerator CoConnectThen(Action onSuccess, Action onFail)
    {
        var nm = NetworkManager.Instance;
        if (nm == null) { onFail?.Invoke(); yield break; }

        if (!nm.IsLoggedIn)
        {
            if (nm.Session == null || !nm.Session.IsConnected)
                nm.Reconnect();

            float elapsed = 0f;
            while (!nm.IsLoggedIn && elapsed < CONNECT_TIMEOUT)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!nm.IsLoggedIn)
            {
                SetButtonsInteractable(true);
                onFail?.Invoke();
                yield break;
            }
        }

        onSuccess?.Invoke();
    }

    void SetButtonsInteractable(bool interactable)
    {
        _btnNewGame.interactable = interactable;
        _btnLoad   .interactable = interactable;
        _btnCoOp   .interactable = interactable;
        _btnQuit   .interactable = interactable;
    }
}
