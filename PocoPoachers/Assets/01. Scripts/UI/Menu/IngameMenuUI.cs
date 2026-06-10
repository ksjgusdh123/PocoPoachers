using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  IngameMenu  [Canvas / CanvasGroup]
//  ├── Dimmer                  [Image]
//  ├── MainPanel               [HorizontalLayoutGroup]
//  │   ├── LeftPanel           [VerticalLayoutGroup]
//  │   │   ├── Header_Team     [TextMeshProUGUI]
//  │   │   └── TeamPanel       [TeamPanelUI]
//  │   └── RightPanel          [VerticalLayoutGroup]
//  │       ├── Btn_Resume
//  │       ├── Btn_Options
//  │       ├── Btn_ReturnToMain
//  │       └── Btn_Quit
// ────────────────────────────────────────────────────────────────────────

public class IngameMenuUI : UIBase
{
    [Header("Left Panel")]
    [SerializeField] private TeamPanelUI _teamPanel;

    [Header("Right Panel Buttons")]
    [SerializeField] private Button _btnResume;
    [SerializeField] private Button _btnOptions;
    [SerializeField] private Button _btnReturnToMain;
    [SerializeField] private Button _btnQuit;

    protected override UIType UiType => UIType.IngameMenu;

    protected override void Awake()
    {
        base.Awake();

        _btnResume        .onClick.AddListener(OnClickResume);
        _btnOptions       .onClick.AddListener(OnClickOptions);
        _btnReturnToMain .onClick.AddListener(OnClickReturnToMain);
        _btnQuit          .onClick.AddListener(OnClickQuit);

        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnDisconnected += OnDisconnected;

        RoomManager.OnGuestLeft += OnGuestLeft;
        RoomManager.OnHostLeft  += OnHostLeft;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();

        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnDisconnected -= OnDisconnected;

        RoomManager.OnGuestLeft -= OnGuestLeft;
        RoomManager.OnHostLeft  -= OnHostLeft;
    }

    private void OnClickResume() => Hide();

    private void OnClickOptions()
    {
        // TODO: Options 패널 연결
    }

    private void OnClickReturnToMain()
    {
        UIManager.GetInstance().ShowWarning(
            "메인화면으로 돌아가기",
            "메인화면으로 돌아가시겠습니까?",
            onConfirm: () =>
            {
                Hide();
                SceneLoader.Instance.LoadTitleScene();
            }
        );
    }

    private void OnClickQuit()
    {
        UIManager.GetInstance().ShowWarning(
            "게임 종료",
            "게임을 종료하시겠습니까?",
            onConfirm: () =>
            {
                RoomManager.Instance?.LeaveRoom();
                NetworkManager.Instance?.LeaveGame();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        );
    }

    private void OnHostLeft()
    {
        UIManager.GetInstance().HideAll();
        UIManager.GetInstance().ShowWarning(
            "호스트 나감",
            "호스트가 게임을 나갔습니다.\n메인화면으로 돌아가시겠습니까?",
            onConfirm: () => SceneLoader.Instance.LoadTitleScene(),
            onCancel:  () => { /* TODO: 호스트 재입장 대기 처리 */ }
        );
    }

    private void OnGuestLeft(int guestId)
    {
        UIManager.GetInstance().HideAll();
        UIManager.GetInstance().ShowWarning(
            "플레이어 나감",
            "플레이어가 게임을 나갔습니다.\n메인화면으로 돌아가시겠습니까?",
            onConfirm: () => SceneLoader.Instance.LoadTitleScene(),
            onCancel:  () => { }
        );
    }

    private void OnDisconnected()
    {
        UIManager.GetInstance().HideAll();
        UIManager.GetInstance().ShowWarning(
            "연결 끊김",
            "서버와의 연결이 끊겼습니다.\n메인화면으로 돌아가시겠습니까?",
            onConfirm: () => SceneLoader.Instance.LoadTitleScene()
        );
    }
}
