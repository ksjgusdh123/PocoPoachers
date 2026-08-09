using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 대화 UI — 열리면 다른 모든 UI를 닫고 이 화면만 띄운다.
// 열려있는 동안은 전용 Dialogue 입력맵으로 전환되어, Advance 액션(F)으로 다음 대사로 넘기거나 닫는다.
// 대사에 선택지가 있으면(DialogueChoiceTable) 대사 텍스트에는 안 넣고 OnChoicesChanged로 목록만 알려준다 —
// 실제로 화면에 어떻게 보여줄지는 별도 선택지 UI가 이 이벤트를 구독해서 그린다.
public class DialogueUI : UIBase
{
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _dialogueText;

    [Serializable]
    private class ChoiceSlot
    {
        [Tooltip("클릭하면 이 슬롯의 선택지를 고른다. 이 순번에 해당하는 선택지가 없으면 이 버튼 자체가 비활성화된다")]
        public Button button;
        [Tooltip("버튼 자식의 텍스트 — 선택지 문구가 채워진다")]
        public TextMeshProUGUI text;
    }

    [Tooltip("선택지마다 하나씩 매칭되는 버튼 슬롯들. 0번=첫 번째 선택지(예: 수락), 1번=두 번째 선택지(예: 거절)... " +
             "그 순번에 해당하는 선택지가 없으면 버튼이 꺼진다.")]
    [SerializeField] private ChoiceSlot[] _choiceSlots;

    // 현재 줄의 선택지 목록이 바뀔 때마다 호출된다 (빈 리스트면 선택지 없음 — 선택지 UI를 숨기면 됨)
    public event Action<IReadOnlyList<DialogueChoiceData>> OnChoicesChanged;

    private int _nextId; // 0이면 다음 대사 없음 — Advance 누르면 닫힘
    private List<DialogueChoiceData> _pendingChoices = new();
    private PlayerController _player;

    protected override UIType UiType => UIType.Dialogue;

    // 대화 중에도 뒷화면이 선명하게 보여야 한다.
    protected override bool UseBackdropBlurByDefault => false;

    protected override void Awake()
    {
        base.Awake(); // UIBase의 자기 등록을 반드시 먼저 태워야 한다

        if (_choiceSlots == null) return;

        for (int i = 0; i < _choiceSlots.Length; i++)
        {
            int index = i; // 클로저 캡처용
            if (_choiceSlots[i]?.button != null)
                _choiceSlots[i].button.onClick.AddListener(() => SelectChoice(index));
        }
    }

    private void Update()
    {
        if (_pendingChoices.Count == 0 || Keyboard.current == null) return;

        for (int i = 0; i < _pendingChoices.Count; i++)
        {
            if (Keyboard.current[Key.Digit1 + i].wasPressedThisFrame)
            {
                SelectChoice(i);
                return;
            }
        }
    }

    // 고정 텍스트로 바로 연다 (단순 알림용) — 다음 대사/선택지 없이 한 줄만 표시. 입력맵 전환은 하지 않는다.
    public void Open(string speakerName, string dialogue)
    {
        UIManager.GetInstance().HideAll();
        SetContent(speakerName, dialogue);
        Show();

        _nextId = 0;
        SetChoices(new List<DialogueChoiceData>());
        _player = null;
    }

    // 대사 테이블의 특정 id부터 시작 (NPC 상호작용용) — Dialogue 입력맵으로 전환해 F(Advance)를 받는다.
    public void OpenById(int dialogueId, PlayerController player)
    {
        DialogueData line = DialogueTable.Instance.Get(dialogueId);
        if (line == null) return;

        UIManager.GetInstance().HideAll();
        Show();

        _player = player;
        if (_player != null)
        {
            _player.SwitchInputMap(PlayerInputMapType.Dialogue);
            _player.InputHandler.DialogueAdvance += Advance;
        }

        ShowLine(line);
    }

    // 이미 열려있는 상태에서 내용만 갱신할 때
    public void SetContent(string speakerName, string dialogue)
    {
        if (_nameText != null) _nameText.text = speakerName;
        if (_dialogueText != null) _dialogueText.text = dialogue;
    }

    // 외부 선택지 UI가 버튼 클릭 등으로 호출 — index는 현재 선택지 목록(OnChoicesChanged로 받은 것) 기준
    public void SelectChoice(int index)
    {
        if (index < 0 || index >= _pendingChoices.Count) return;

        DialogueChoiceData choice = _pendingChoices[index];
        SetChoices(new List<DialogueChoiceData>());

        // 선택지에 accept_quest_id가 있으면(dialogue_choice.csv) 그 퀘스트를 수락한다.
        // 로컬에 먼저 낙관적으로 적용하고 RoomSync.QuestAccept로 전파한다(ShelterManager.TryUpgrade와 동일 패턴) -
        // 호스트면 전원에게 브로드캐스트, 게스트면 호스트에게 보내서 호스트가 확인 후 다시 전원에게 브로드캐스트한다.
        if (choice.AcceptQuestId > 0)
        {
            QuestManager.Accept(choice.AcceptQuestId);
            RoomSync.QuestAccept(choice.AcceptQuestId);
        }

        DialogueData next = choice.NextId > 0 ? DialogueTable.Instance.Get(choice.NextId) : null;
        if (next == null)
        {
            Hide();
            return;
        }

        ShowLine(next);
    }

    private void Advance()
    {
        if (_pendingChoices.Count > 0) return; // 선택지 표시 중엔 F 무시

        DialogueData next = _nextId > 0 ? DialogueTable.Instance.Get(_nextId) : null;
        if (next == null)
        {
            Hide();
            return;
        }

        ShowLine(next);
    }

    // 대사 한 줄을 표시하고, 그 줄에 딸린 선택지 목록을 OnChoicesChanged로 알린다
    private void ShowLine(DialogueData line)
    {
        _nextId = line.NextId;
        SetContent(line.Speaker, line.Text);
        SetChoices(GetChoices(line.Id));
    }

    private void SetChoices(List<DialogueChoiceData> choices)
    {
        _pendingChoices = choices;

        if (_choiceSlots != null)
        {
            for (int i = 0; i < _choiceSlots.Length; i++)
            {
                ChoiceSlot slot = _choiceSlots[i];
                if (slot == null) continue;

                bool hasChoice = i < choices.Count;
                if (slot.button != null) slot.button.gameObject.SetActive(hasChoice);
                if (hasChoice && slot.text != null) slot.text.text = $"{i + 1}. {choices[i].Text}";
            }
        }

        OnChoicesChanged?.Invoke(_pendingChoices);
    }

    private static List<DialogueChoiceData> GetChoices(int dialogueId) =>
        DialogueChoiceTable.Instance.All
            .Where(c => c.DialogueId == dialogueId)
            .OrderBy(c => c.Order)
            .ToList();

    protected override void OnHide()
    {
        SetChoices(new List<DialogueChoiceData>());

        if (_player != null)
        {
            _player.InputHandler.DialogueAdvance -= Advance;
            _player.InputHandler.SwitchToGameplayMapNextFrame();
            _player = null;
        }
    }
}
