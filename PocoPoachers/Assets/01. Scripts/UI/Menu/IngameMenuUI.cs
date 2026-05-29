using System;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  IngameMenu  [Canvas / CanvasGroup]
//  ├── Dimmer                  [Image]
//  ├── MainPanel               [HorizontalLayoutGroup]
//  │   ├── LeftPanel           [VerticalLayoutGroup]
//  │   │   ├── Header_Team     [TextMeshProUGUI]
//  │   │   └── TeamPanel       [TeamPanelUI]
//  │   │       ├── PlayerEntry_0~3  [GameObject]
//  │   │       ├── Btn_Invite       [Button]
//  │   │       ├── Txt_Status       [TextMeshProUGUI] (선택)
//  │   │       ├── Txt_Code         [TextMeshProUGUI]
//  │   │       └── Btn_Copy         [Button]
//  │   └── RightPanel          [VerticalLayoutGroup]
//  │       ├── Btn_Resume
//  │       ├── Btn_Options
//  │       ├── Btn_ReturnToLobby
//  │       └── Btn_Quit
//  └── WarningPopup            [GameObject] — 기본 비활성화
//      ├── Popup_Dimmer        [Image]
//      └── Popup_Panel
//          ├── Txt_Message     [TextMeshProUGUI]
//          ├── Btn_Confirm
//          └── Btn_Cancel
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

    [Header("Warning Popup")]
    [SerializeField] private WarningPopupUI _warningPopup;

    private Action _pendingConfirmAction;

    private void Awake()
    {
        UIManager.GetInstance().Register(UIType.IngameMenu, gameObject);

        _btnResume        .onClick.AddListener(OnClickResume);
        _btnOptions       .onClick.AddListener(OnClickOptions);
        _btnReturnToLobby .onClick.AddListener(OnClickReturnToLobby);
        _btnQuit          .onClick.AddListener(OnClickQuit);

        _warningPopup.OnConfirmed += OnPopupConfirm;
        _warningPopup.OnCancelled += OnPopupCancel;

        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        var ui = UIManager.GetInstance();
        if (ui == null) return;
        ui.Unregister(UIType.IngameMenu);

        _warningPopup.OnConfirmed -= OnPopupConfirm;
        _warningPopup.OnCancelled -= OnPopupCancel;
    }

    private void OnClickResume() => UIManager.GetInstance().Hide(UIType.IngameMenu);

    private void OnClickOptions()
    {
        // TODO: Options 패널 연결
    }

    private void OnClickReturnToLobby()
    {
        _pendingConfirmAction = () =>
        {
            UIManager.GetInstance().Hide(UIType.IngameMenu);
            SceneLoader.Instance.LoadLobbyScene();
        };
        _warningPopup.Show("로비로 돌아가기", "아이템이 소실됩니다.\n계속하시겠습니까?");
    }

    private void OnClickQuit()
    {
        _pendingConfirmAction = () =>
        {
            NetworkManager.Instance?.LeaveGame();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        };
        _warningPopup.Show("게임 종료", "게임을 종료하시겠습니까?");
    }

    private void OnPopupConfirm()
    {
        _pendingConfirmAction?.Invoke();
        _pendingConfirmAction = null;
    }

    private void OnPopupCancel() => _pendingConfirmAction = null;
}
