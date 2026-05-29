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
//  │       ├── Btn_ReturnToLobby
//  │       └── Btn_Quit
// ────────────────────────────────────────────────────────────────────────

public class IngameMenuUI : MonoBehaviour
{
    [Header("Left Panel")]
    [SerializeField] private TeamPanelUI _teamPanel;

    [Header("Right Panel Buttons")]
    [SerializeField] private Button _btnResume;
    [SerializeField] private Button _btnOptions;
    [SerializeField] private Button _btnReturnToLobby;
    [SerializeField] private Button _btnQuit;

    private void Awake()
    {
        UIManager.GetInstance().Register(UIType.IngameMenu, gameObject);

        _btnResume        .onClick.AddListener(OnClickResume);
        _btnOptions       .onClick.AddListener(OnClickOptions);
        _btnReturnToLobby .onClick.AddListener(OnClickReturnToLobby);
        _btnQuit          .onClick.AddListener(OnClickQuit);

        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnDisconnected += OnDisconnected;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        var ui = UIManager.GetInstance();
        if (ui == null) return;
        ui.Unregister(UIType.IngameMenu);

        if (NetworkManager.Instance != null)
            NetworkManager.Instance.OnDisconnected -= OnDisconnected;
    }

    private void OnClickResume() => UIManager.GetInstance().Hide(UIType.IngameMenu);

    private void OnClickOptions()
    {
        // TODO: Options 패널 연결
    }

    private void OnClickReturnToLobby()
    {
        UIManager.GetInstance().ShowWarning(
            "로비로 돌아가기",
            "아이템이 소실됩니다.\n계속하시겠습니까?",
            onConfirm: () =>
            {
                UIManager.GetInstance().Hide(UIType.IngameMenu);
                SceneLoader.Instance.LoadLobbyScene();
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
                NetworkManager.Instance?.LeaveGame();
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
            }
        );
    }

    private void OnDisconnected()
    {
        UIManager.GetInstance().HideAll();
        UIManager.GetInstance().ShowWarning(
            "연결 끊김",
            "서버와의 연결이 끊겼습니다.\n로비로 돌아가시겠습니까?",
            onConfirm: () => SceneLoader.Instance.LoadLobbyScene()
        );
    }
}
