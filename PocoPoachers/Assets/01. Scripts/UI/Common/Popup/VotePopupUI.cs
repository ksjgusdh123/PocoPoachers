using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// ── Inspector 연결 구조 ──────────────────────────────────────────────────
//  VotePopupUI [UIPopupFrame]
//  └── Content
//      ├── txtMessage  [TextMeshProUGUI]
//      ├── Roster      [HorizontalLayoutGroup]  ← VoteMemberIcon 프리팹을 인원 수만큼 생성
//      ├── txtProgress [TextMeshProUGUI]
//      └── group
//          ├── btnAccept  [Button] └ Txt_Accept  [TextMeshProUGUI]
//          └── btnDecline [Button] └ Txt_Decline [TextMeshProUGUI]
// ────────────────────────────────────────────────────────────────────────

// 팀 씬 이동 투표 팝업. 게스트는 수락/거절을 고르고, 호스트는 같은 팝업을 응답 대기 화면으로 쓴다
// (수락 버튼을 숨기고 거절 버튼이 취소 역할).
public class VotePopupUI : PopupUIBase
{
    [SerializeField] private TextMeshProUGUI _txtMessage;
    [SerializeField] private TextMeshProUGUI _txtProgress;
    [SerializeField] private Button          _btnAccept;
    [SerializeField] private Button          _btnDecline;
    [SerializeField] private TextMeshProUGUI _txtAccept;
    [SerializeField] private TextMeshProUGUI _txtDecline;

    [Header("Roster")]
    [SerializeField] private RectTransform    _roster;
    [SerializeField] private VoteMemberIconUI _memberIconPrefab;

    // 전투 중에 마우스로 옮겨가지 않고도 답할 수 있도록 단축키를 함께 받는다.
    [Header("Hotkeys")]
    [SerializeField]
    private InputAction _acceptAction = new InputAction("VoteAccept", InputActionType.Button, AcceptBinding);
    [SerializeField]
    private InputAction _declineAction = new InputAction("VoteDecline", InputActionType.Button, DeclineBinding);

    private const string AcceptBinding  = "<Keyboard>/y";
    private const string DeclineBinding = "<Keyboard>/n";

    // 게스트에게 응답을 받는 중인지, 이미 답했는지. ESC로 닫히면 거절로 처리하기 위해 필요하다.
    private bool _requestMode;
    private bool _answered;

    // 생성한 아이콘은 파괴하지 않고 재사용한다 — 투표를 열 때마다 Instantiate/Destroy를 반복할 이유가 없다.
    private readonly List<VoteMemberIconUI> _memberIcons = new();

    public event Action OnAccepted;
    public event Action OnDeclined;

    protected override UIType UiType => UIType.VotePopup;

    protected override TextMeshProUGUI MessageText => _txtMessage;

    protected override void Awake()
    {
        base.Awake();

        _btnAccept .onClick.AddListener(Accept);
        _btnDecline.onClick.AddListener(Decline);

        // 인스펙터에서 바인딩이 비어 있어도 기본 키로 동작하게 보정한다.
        if (_acceptAction.bindings.Count == 0)  _acceptAction.AddBinding(AcceptBinding);
        if (_declineAction.bindings.Count == 0) _declineAction.AddBinding(DeclineBinding);

        _acceptAction.performed  += _ => Accept();
        _declineAction.performed += _ => Decline();
    }

    private void OnDisable() => DisableHotkeys();

    protected override void OnShow()
    {
        if (!_requestMode) return;

        _acceptAction.Enable();
        _declineAction.Enable();
    }

    // ESC나 다른 UI에 밀려 닫히는 경우도 여기로 온다. 답하지 않고 닫혔으면 거절로 처리해야
    // 호스트가 타임아웃까지 기다리지 않는다.
    protected override void OnHide()
    {
        DisableHotkeys();
        if (!_requestMode || _answered) return;

        _answered = true;
        OnDeclined?.Invoke();
    }

    private void Accept()
    {
        if (_answered) return;

        _answered = true;
        OnAccepted?.Invoke();
    }

    private void Decline()
    {
        if (_answered) return;

        _answered = true;
        OnDeclined?.Invoke();
    }

    private void DisableHotkeys()
    {
        _acceptAction.Disable();
        _declineAction.Disable();
    }

    // 게스트 — 수락/거절 두 버튼
    public void SetRequestMode()
    {
        var localization = LocalizationManager.GetInstance();
        _requestMode = true;
        _answered    = false;
        _btnAccept.gameObject.SetActive(true);
        _txtAccept .text = localization.GetString("vote.accept");
        _txtDecline.text = localization.GetString("vote.decline");
    }

    // 호스트 — 응답을 기다리는 동안 취소만 가능
    public void SetWaitingMode()
    {
        _requestMode = false;
        _answered    = false;
        _btnAccept.gameObject.SetActive(false);
        _txtDecline.text = LocalizationManager.GetInstance().GetString("common.cancel");
    }

    public void SetProgress(string text)
    {
        if (_txtProgress != null) _txtProgress.text = text;
    }

    // 팀 인원 수(호스트 포함)만큼 아이콘을 만들고(모자라면 생성) 전부 미수락 상태로 되돌린다.
    public void SetMemberCount(int count)
    {
        if (_roster != null) _roster.gameObject.SetActive(count > 0);
        if (_memberIconPrefab == null || _roster == null) return;

        while (_memberIcons.Count < count)
            _memberIcons.Add(Instantiate(_memberIconPrefab, _roster));

        for (int i = 0; i < _memberIcons.Count; i++)
        {
            bool used = i < count;
            _memberIcons[i].gameObject.SetActive(used);
            if (used) _memberIcons[i].SetAccepted(false);
        }
    }

    public void MarkMemberAccepted(int index)
    {
        if (!TryGetIcon(index, out var icon)) return;
        icon.SetAccepted(true);
    }

    private bool TryGetIcon(int index, out VoteMemberIconUI icon)
    {
        icon = null;
        if (index < 0 || index >= _memberIcons.Count) return false;

        icon = _memberIcons[index];
        return icon != null;
    }

    protected override void RegisterToManager() => UIManager.GetInstance().RegisterVotePopup(this);
    protected override void UnregisterSelf()    => UIManager.GetInstance().UnregisterVotePopup();
}
