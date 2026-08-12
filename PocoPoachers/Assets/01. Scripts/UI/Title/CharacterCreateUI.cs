using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  NicknamePanel [CharacterCreateUI]
//  ├─ InputNickname (TMP_InputField)
//  ├─ BtnConfirm    (Button)
//  └─ BtnBack       (Button)
// ────────────────────────────────────────────────────────────────────────
//
// 새 게임의 캐릭터 생성 화면(SC_CharacterCreate). 닉네임을 확정하면 새 세이브 슬롯을 만들어
// 기록하고, 타이틀의 새 게임과 동일하게 호스트 세션을 시작한다(이후 RoomManager가 쉘터로 보낸다).
// 외형 커스터마이징도 이 화면에 붙일 예정이라 "확정" 시점을 여기 한 곳으로 모아둔다.
public class CharacterCreateUI : MonoBehaviour
{
    public const int MinNicknameLength = 2;
    public const int MaxNicknameLength = 12;

    [SerializeField] private TMP_InputField _inputNickname;
    [SerializeField] private Button _btnConfirm;
    [SerializeField] private Button _btnBack;

    private string Nickname => _inputNickname.text.Trim();

    private void Awake()
    {
        _inputNickname.characterLimit = MaxNicknameLength;
        _inputNickname.onValueChanged.AddListener(_ => RefreshConfirm());
        _inputNickname.onSubmit.AddListener(_ => OnClickConfirm());

        _btnConfirm.onClick.AddListener(OnClickConfirm);
        _btnBack.onClick.AddListener(OnClickBack);

        RoomManager.Instance.OnRoomJoinFailed += HandleRoomFailed;
    }

    private void OnEnable()
    {
        _inputNickname.text = "";
        _inputNickname.ActivateInputField();
        RefreshConfirm();
    }

    private void OnDestroy()
    {
        if (RoomManager.Instance != null)
            RoomManager.Instance.OnRoomJoinFailed -= HandleRoomFailed;
    }

    private void RefreshConfirm() => _btnConfirm.interactable = Nickname.Length >= MinNicknameLength;

    private void OnClickConfirm()
    {
        string nickname = Nickname;
        if (nickname.Length < MinNicknameLength) return;

        SetInteractable(false);

        var save = SaveManager.GetInstance();
        save.AllocateNewSlot();
        save.SaveNickname(nickname);
        save.LoadEquipmentState(); // 새 슬롯: 장비 상태 초기화 + uid 카운터 리셋
        save.LoadQuestState();     // 새 슬롯: 퀘스트 진행 상태 초기화

        var loc = LocalizationManager.GetInstance();
        StartCoroutine(NetworkConnectFlow.Run(
            onSuccess: () => RoomManager.Instance.StartAsHost(),
            onFail: () => UIManager.GetInstance().ShowWarning(
                loc.GetString("network.connect_failed_title"),
                loc.GetString("network.local_play_fallback"),
                onConfirm: () => RoomManager.Instance.StartLocalHost(),
                onCancel: () => SetInteractable(true))));
    }

    // 아직 방을 만들기 전이라 세션 정리 없이 타이틀로 돌아가면 된다
    private void OnClickBack() => SceneManager.LoadScene(SceneName.Title);

    private void HandleRoomFailed(string _) => SetInteractable(true);

    private void SetInteractable(bool interactable)
    {
        _inputNickname.interactable = interactable;
        _btnBack.interactable = interactable;
        _btnConfirm.interactable = interactable && Nickname.Length >= MinNicknameLength;
    }
}
