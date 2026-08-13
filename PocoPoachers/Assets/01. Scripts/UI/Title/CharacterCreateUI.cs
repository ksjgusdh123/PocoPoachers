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
    private const int MinLength = 2;
    private const int MaxLength = 12;

    // 참여한 방에 내 기록이 없어(최초 등록) 이름만 만들러 온 경우.
    // 이미 접속은 끝난 상태라, 확정하면 새 게임을 시작하는 대신 이름을 호스트에 보고하고 쉘터로 들어간다.
    public static bool JoinFlow;

    [SerializeField] private TMP_InputField _inputNickname;
    [SerializeField] private Button _btnConfirm;
    [SerializeField] private Button _btnBack;

    private string Nickname => _inputNickname.text.Trim();

    private void Awake()
    {
        _inputNickname.characterLimit = MaxLength;
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

    private void RefreshConfirm() => _btnConfirm.interactable = Nickname.Length >= MinLength;

    private void OnClickConfirm()
    {
        string nickname = Nickname;
        if (nickname.Length < MinLength) return;

        // 참여한 방의 최초 등록 — 세이브 슬롯은 만들지 않는다. 이름을 호스트에 보고하고 그대로 입장한다.
        if (JoinFlow)
        {
            JoinFlow = false;
            SaveManager.GetInstance().SaveLastNickname(nickname);
            RoomSync.Nickname(nickname);
            RoomManager.Instance.EnterShelterFromMenu();
            return;
        }

        SetInteractable(false);
        StartCoroutine(NewGameStart.Run(nickname, onCancel: () => SetInteractable(true)));
    }

    private void OnClickBack()
    {
        // 참여 중이었다면 이미 방에 붙어 있으므로 세션을 정리하고 나간다
        if (JoinFlow)
        {
            JoinFlow = false;
            SceneLoader.Instance.LoadTitleScene();
            return;
        }

        SceneManager.LoadScene(SceneName.Title);
    }

    private void HandleRoomFailed(string _) => SetInteractable(true);

    private void SetInteractable(bool interactable)
    {
        _inputNickname.interactable = interactable;
        _btnBack.interactable = interactable;
        _btnConfirm.interactable = interactable && Nickname.Length >= MinLength;
    }
}
