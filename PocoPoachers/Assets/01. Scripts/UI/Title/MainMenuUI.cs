using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  MainMenuUI
//  ├─ BtnNewGame        (Button)
//  ├─ BtnLoad           (Button)
//  ├─ BtnCoOp           (Button)
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
    private const float CONNECT_TIMEOUT = 5f;

    [SerializeField] Button     _btnNewGame;
    [SerializeField] Button     _btnLoad;
    [SerializeField] Button     _btnCoOp;
    [SerializeField] Button     _btnQuit;
    [SerializeField] JoinCodeUI _coOpUI;
    [SerializeField] GameObject _panelSaveSlots;
    [SerializeField] Button     _btnCloseSaveSlots;

    void Awake()
    {
        RoomManager.Instance.OnGameStarted    += OnGameStarted;
        RoomManager.Instance.OnRoomJoinFailed += OnRoomJoinFailed;
        SaveSlotButtonUI.OnSlotSelected       += OnSaveSlotSelected;

        _btnNewGame.onClick.AddListener(OnClickNewGame);
        _btnLoad   .onClick.AddListener(OnClickLoad);
        _btnCoOp   .onClick.AddListener(OnClickCoOp);
        _btnQuit   .onClick.AddListener(OnClickQuit);
        _btnCloseSaveSlots?.onClick.AddListener(CloseSaveSlotPanel);

        _coOpUI.gameObject.SetActive(false);
        _panelSaveSlots?.SetActive(false);
    }

    void OnDestroy()
    {
        if (RoomManager.Instance != null)
        {
            RoomManager.Instance.OnGameStarted    -= OnGameStarted;
            RoomManager.Instance.OnRoomJoinFailed -= OnRoomJoinFailed;
        }
        SaveSlotButtonUI.OnSlotSelected -= OnSaveSlotSelected;
    }

    void OnGameStarted()
    {
        GameManager.Instance.SetSpawnId(SpawnId.FromTitle);
        SceneLoader.Instance.LoadShelterScene();
    }

    void OnClickNewGame()
    {
        SaveManager.GetInstance().AllocateNewSlot();
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
        _panelSaveSlots?.SetActive(true);
    }

    void OnSaveSlotSelected(int slotIndex)
    {
        CloseSaveSlotPanel();
        SaveManager.GetInstance().SetActiveSlot(slotIndex);
        GameManager.GetInstance().SetLoadPlayerInventory(true);
        SetButtonsInteractable(false);
        StartCoroutine(CoConnectThen(
            onSuccess: () => RoomManager.Instance.StartAsHost(),
            onFail: () => UIManager.GetInstance().ShowWarning(
                "연결 실패",
                "서버에 연결할 수 없습니다.\n로컬 플레이로 진행하시겠습니까?",
                onConfirm: () => RoomManager.Instance.StartLocalHost(),
                onCancel:  () =>
                {
                    GameManager.GetInstance().SetLoadPlayerInventory(false);
                    SetButtonsInteractable(true);
                }
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
