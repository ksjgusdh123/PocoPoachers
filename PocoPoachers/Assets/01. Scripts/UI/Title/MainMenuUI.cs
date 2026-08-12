using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  MainMenuUI
//  ├─ BtnNewGame        (Button)
//  ├─ BtnLoad           (Button)
//  ├─ BtnCoOp           (Button)
//  ├─ BtnOption         (Button)
//  ├─ BtnQuit           (Button)
//  ├─ CoOpUI            (JoinCodeUI)
//  └─ PanelSaveSlots    (GameObject)
//      ├─ SaveSlot_0    (SaveSlotUI)  slotIndex=0
//      ├─ SaveSlot_1    (SaveSlotUI)  slotIndex=1
//      ├─ SaveSlot_2    (SaveSlotUI)  slotIndex=2
//      └─ BtnClose      (Button)
// ────────────────────────────────────────────────────────────────────────

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] Button     _btnNewGame;
    [SerializeField] Button     _btnLoad;
    [SerializeField] Button     _btnCoOp;
    [SerializeField] Button     _btnOption;
    [SerializeField] Button     _btnQuit;
    [SerializeField] JoinCodeUI _coOpUI;
    [SerializeField] GameObject _panelSaveSlots;
    [SerializeField] Button     _btnCloseSaveSlots;

    void Awake()
    {
        SoundManager.GetInstance().PlayBgm("bgm_main");

        RoomManager.Instance.OnRoomJoinFailed += OnRoomJoinFailed;
        SaveSlotButtonUI.OnSlotSelected       += OnSaveSlotSelected;

        _btnNewGame.onClick.AddListener(OnClickNewGame);
        _btnLoad   .onClick.AddListener(OnClickLoad);
        _btnCoOp   .onClick.AddListener(OnClickCoOp);
        _btnOption .onClick.AddListener(OnClickOption);
        _btnQuit   .onClick.AddListener(OnClickQuit);
        _btnCloseSaveSlots?.onClick.AddListener(CloseSaveSlotPanel);

        _coOpUI.gameObject.SetActive(false);
        _panelSaveSlots?.SetActive(false);
    }

    void OnDestroy()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnRoomJoinFailed -= OnRoomJoinFailed;
        SaveSlotButtonUI.OnSlotSelected -= OnSaveSlotSelected;
    }

    // 슬롯 할당/닉네임 확정과 호스트 시작은 캐릭터 생성 씬(CharacterCreateUI)이 이어서 처리한다
    void OnClickNewGame() => SceneManager.LoadScene(SceneName.CharacterCreate);

    void OnClickLoad()
    {
        _panelSaveSlots?.SetActive(true);
    }

    void OnSaveSlotSelected(int slotIndex)
    {
        CloseSaveSlotPanel();
        SaveManager.GetInstance().SetActiveSlot(slotIndex);
        SaveManager.GetInstance().LoadEquipmentState(); // 저장된 장비 상태 복원 + uid 카운터 시드
        SaveManager.GetInstance().LoadQuestState(); // 저장된 퀘스트 진행 상태 복원
        SetButtonsInteractable(false);
        StartCoroutine(CoConnectThen(
            onSuccess: () => RoomManager.Instance.StartAsHost(),
            onFail: () => UIManager.GetInstance().ShowWarning(
                LocalizationManager.GetInstance().GetString("network.connect_failed_title"),
                LocalizationManager.GetInstance().GetString("network.local_play_fallback"),
                onConfirm: () => RoomManager.Instance.StartLocalHost(),
                onCancel:  () => SetButtonsInteractable(true)
            )
        ));
    }

    void CloseSaveSlotPanel() => _panelSaveSlots?.SetActive(false);

    void OnClickCoOp()
    {
        SetButtonsInteractable(false);
        StartCoroutine(CoConnectThen(
            onSuccess: () => { SetButtonsInteractable(true); _coOpUI.Show(); },
            onFail:    () =>
            {
                SetButtonsInteractable(true);
                UIManager.GetInstance().ShowNotice(
                    LocalizationManager.GetInstance().GetString("network.connect_failed_title"),
                    LocalizationManager.GetInstance().GetString("network.coop_requires_server"));
            }
        ));
    }

    void OnClickOption() => UIManager.GetInstance().Show(UIType.Options);

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

    IEnumerator CoConnectThen(Action onSuccess, Action onFail) =>
        NetworkConnectFlow.Run(onSuccess, () =>
        {
            SetButtonsInteractable(true);
            onFail?.Invoke();
        });

    void SetButtonsInteractable(bool interactable)
    {
        _btnNewGame.interactable = interactable;
        _btnLoad   .interactable = interactable;
        _btnCoOp   .interactable = interactable;
        _btnQuit   .interactable = interactable;
    }
}
