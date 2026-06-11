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

    private void OnClickOptions() => UIManager.GetInstance().Show(UIType.Options);

    private void OnClickReturnToMain()
    {
        UIManager.GetInstance().ShowWarning(
            LocalizationManager.GetInstance().GetString("menu.return_to_title_title"),
            LocalizationManager.GetInstance().GetString("menu.return_to_title_message"),
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
            LocalizationManager.GetInstance().GetString("menu.quit_game_title"),
            LocalizationManager.GetInstance().GetString("menu.quit_game_message"),
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
            LocalizationManager.GetInstance().GetString("menu.host_left_title"),
            LocalizationManager.GetInstance().GetString("menu.host_left_message"),
            onConfirm: () => SceneLoader.Instance.LoadTitleScene(),
            onCancel:  () => { /* TODO: 호스트 재입장 대기 처리 */ }
        );
    }

    private void OnGuestLeft(int guestId)
    {
        UIManager.GetInstance().HideAll();
        UIManager.GetInstance().ShowWarning(
            LocalizationManager.GetInstance().GetString("menu.guest_left_title"),
            LocalizationManager.GetInstance().GetString("menu.guest_left_message"),
            onConfirm: () => SceneLoader.Instance.LoadTitleScene(),
            onCancel:  () => { }
        );
    }

    private void OnDisconnected()
    {
        UIManager.GetInstance().HideAll();
        UIManager.GetInstance().ShowWarning(
            LocalizationManager.GetInstance().GetString("menu.disconnected_title"),
            LocalizationManager.GetInstance().GetString("menu.disconnected_message"),
            onConfirm: () => SceneLoader.Instance.LoadTitleScene()
        );
    }
}
