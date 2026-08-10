using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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

    // 생성한 아이콘은 파괴하지 않고 재사용한다 — 투표를 열 때마다 Instantiate/Destroy를 반복할 이유가 없다.
    private readonly List<VoteMemberIconUI> _memberIcons = new();

    public event Action OnAccepted;
    public event Action OnDeclined;

    protected override UIType UiType => UIType.VotePopup;

    protected override TextMeshProUGUI MessageText => _txtMessage;

    protected override void Awake()
    {
        base.Awake();

        _btnAccept .onClick.AddListener(() => OnAccepted?.Invoke());
        _btnDecline.onClick.AddListener(() => OnDeclined?.Invoke());
    }

    // 게스트 — 수락/거절 두 버튼. 남의 응답은 알 수 없으므로 인원 아이콘은 숨긴다.
    public void SetRequestMode()
    {
        var localization = LocalizationManager.GetInstance();
        _btnAccept.gameObject.SetActive(true);
        _txtAccept .text = localization.GetString("vote.accept");
        _txtDecline.text = localization.GetString("vote.decline");
        SetMemberCount(0);
    }

    // 호스트 — 응답을 기다리는 동안 취소만 가능
    public void SetWaitingMode()
    {
        _btnAccept.gameObject.SetActive(false);
        _txtDecline.text = LocalizationManager.GetInstance().GetString("common.cancel");
    }

    public void SetProgress(string text)
    {
        if (_txtProgress != null) _txtProgress.text = text;
    }

    // 응답을 기다리는 인원 수만큼 아이콘을 만들고(모자라면 생성) 전부 미수락 상태로 되돌린다.
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

    // 대기 중 나간 인원은 더 기다리지 않으므로 목록에서 뺀다.
    public void MarkMemberGone(int index)
    {
        if (!TryGetIcon(index, out var icon)) return;
        icon.gameObject.SetActive(false);
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
