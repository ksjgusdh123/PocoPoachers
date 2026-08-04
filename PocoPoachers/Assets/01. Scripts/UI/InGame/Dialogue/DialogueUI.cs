using TMPro;
using UnityEngine;

// 대화 UI — 열리면 다른 모든 UI를 닫고 이 화면만 띄운다.
// 열려있는 동안은 전용 Dialogue 입력맵으로 전환되어, Advance 액션(F)으로 다음 대사로 넘기거나 닫는다.
// Advance는 다른 맵의 Interaction과 이름이 다른 별도 액션이라, 월드 상호작용(F)과 서로 재소비되지 않는다.
public class DialogueUI : UIBase
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _dialogueText;

    [Tooltip("대화창이 열릴 때 같이 켜지는 배경(화면 전체를 덮는 어두운 이미지 등). 씬/프리팹에서 직접 배치해서 연결.")]
    [SerializeField] private GameObject _backgroundDim;

    private int _nextId; // 0이면 다음 대사 없음 — Advance 누르면 닫힘
    private PlayerController _player;

    protected override UIType UiType => UIType.Dialogue;

    // 고정 텍스트로 바로 연다 (단순 알림용) — 다음 대사 없이 한 줄만 표시. 입력맵 전환은 하지 않는다.
    public void Open(string speakerName, string dialogue)
    {
        OpenInternal(speakerName, dialogue, nextId: 0, player: null);
    }

    // 대사 테이블의 특정 id부터 시작 (NPC 상호작용용) — Dialogue 입력맵으로 전환해 F(Advance)를 받는다.
    public void OpenById(int dialogueId, PlayerController player)
    {
        DialogueData line = DialogueTable.Instance.Get(dialogueId);
        if (line == null) return;

        OpenInternal(line.Speaker, line.Text, line.NextId, player);
    }

    private void OpenInternal(string speakerName, string dialogue, int nextId, PlayerController player)
    {
        UIManager.GetInstance().HideAll();
        SetContent(speakerName, dialogue);
        Show();
        if (_backgroundDim != null) _backgroundDim.SetActive(true);

        _nextId = nextId;
        _player = player;

        if (_player != null)
        {
            _player.SwitchInputMap(PlayerInputMapType.Dialogue);
            _player.InputHandler.DialogueAdvance += Advance;
        }
    }

    // 이미 열려있는 상태에서 내용만 갱신할 때
    public void SetContent(string speakerName, string dialogue)
    {
        if (_nameText != null) _nameText.text = speakerName;
        if (_dialogueText != null) _dialogueText.text = dialogue;
    }

    private void Advance()
    {
        DialogueData next = _nextId > 0 ? DialogueTable.Instance.Get(_nextId) : null;
        if (next == null)
        {
            Hide();
            return;
        }

        SetContent(next.Speaker, next.Text);
        _nextId = next.NextId;
    }

    protected override void OnHide()
    {
        if (_backgroundDim != null) _backgroundDim.SetActive(false);

        if (_player != null)
        {
            _player.InputHandler.DialogueAdvance -= Advance;
            _player.InputHandler.SwitchToGameplayMapNextFrame();
            _player = null;
        }
    }
}
