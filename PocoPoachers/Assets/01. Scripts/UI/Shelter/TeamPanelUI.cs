using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  TeamPanel [TeamPanelUI]
//  ├── PlayerEntry_0          [GameObject]
//  ├── PlayerEntry_1          [GameObject]
//  ├── PlayerEntry_2          [GameObject]
//  ├── PlayerEntry_3          [GameObject]
//  ├── Btn_Invite             [Button]           — 호스트만 표시
//  ├── Txt_Code               [TextMeshProUGUI]  — 코드 생성 후 표시
//  └── Btn_Copy               [Button]           — 코드 생성 후 표시
// ────────────────────────────────────────────────────────────────────────

public class TeamPanelUI : MonoBehaviour
{
    private const float LOGIN_TIMEOUT = 5f;

    [Header("Team Slots")]
    [SerializeField] private GameObject[] _playerSlots = new GameObject[4];

    [Header("Invite")]
    [SerializeField] private Button          _btnInvite;
    [SerializeField] private TextMeshProUGUI _txtCode;
    [SerializeField] private Button          _btnCopy;

    private void Awake()
    {
        _btnInvite.onClick.AddListener(OnClickInvite);
        _btnCopy  .onClick.AddListener(OnClickCopy);

        _btnInvite.gameObject.SetActive(RoomManager.IsHost);
        _txtCode.gameObject.SetActive(false);
        _btnCopy.gameObject.SetActive(false);

        RoomManager.Instance.OnSessionCodeReceived += OnCodeReceived;
        RoomManager.Instance.OnRoomJoinFailed      += OnRoomJoinFailed;
    }

    private void OnDestroy()
    {
        if (RoomManager.Instance == null) return;
        RoomManager.Instance.OnSessionCodeReceived -= OnCodeReceived;
        RoomManager.Instance.OnRoomJoinFailed      -= OnRoomJoinFailed;
    }

    // ── Team List ──────────────────────────────────────────────────────

    public void RefreshTeamList(int activeMemberCount)
    {
        for (int i = 0; i < _playerSlots.Length; i++)
            _playerSlots[i].SetActive(i < activeMemberCount);
    }

    // ── Invite ─────────────────────────────────────────────────────────

    private void OnClickInvite()
    {
        string code = RoomManager.Instance.SessionCode;
        if (!string.IsNullOrEmpty(code)) { ShowCode(code); return; }

        _btnInvite.interactable = false;
        StartCoroutine(CoConnectAndCreateRoom());
    }

    private IEnumerator CoConnectAndCreateRoom()
    {
        var nm = NetworkManager.Instance;
        if (nm == null)
        {
            _btnInvite.interactable = true;
            yield break;
        }

        if (!nm.IsLoggedIn)
        {
            if (nm.Session == null || !nm.Session.IsConnected)
                nm.Reconnect();

            float elapsed = 0f;
            while (!nm.IsLoggedIn && elapsed < LOGIN_TIMEOUT)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!nm.IsLoggedIn)
            {
                _btnInvite.interactable = true;
                UIManager.GetInstance().ShowNotice(
                    LocalizationManager.GetInstance().GetString("network.connect_failed_title"),
                    LocalizationManager.GetInstance().GetString("network.connect_failed_message"));
                yield break;
            }
        }

        RoomManager.Instance.StartAsHost();
    }

    private void OnCodeReceived(string code)
    {
        _btnInvite.interactable = true;
        ShowCode(code);
    }

    private void OnRoomJoinFailed(string _)
    {
        _btnInvite.interactable = true;
        UIManager.GetInstance().ShowNotice(
            LocalizationManager.GetInstance().GetString("network.invite_failed_title"),
            LocalizationManager.GetInstance().GetString("network.invite_failed_message"));
    }

    private void ShowCode(string code)
    {
        _txtCode.text = code;
        _txtCode.gameObject.SetActive(true);
        _btnCopy.gameObject.SetActive(true);
    }

    private void OnClickCopy() => GUIUtility.systemCopyBuffer = RoomManager.Instance.SessionCode;
}
