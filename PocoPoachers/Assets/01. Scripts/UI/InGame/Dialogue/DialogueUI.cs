using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 대화 UI — 열리면 다른 모든 UI를 닫고 이 화면만 띄운다.
// 열려있는 동안은 전용 Dialogue 입력맵으로 전환되어, Advance 액션(F)으로 다음 대사로 넘기거나 닫는다.
//
// 기본 대사 체인이 끝나면 그 NPC가 가진 퀘스트 목록을 자동으로 띄운다(quest.csv의 npc_id로 묶는다) —
// 퀘스트를 늘려도 dialogue_choice.csv를 손댈 필요가 없다. 목록에서 하나 고르면 그 퀘스트의 상태에 맞는
// 대사(quest.csv의 offer/progress/complete_dialogue_id)로 들어가고, 대사가 끝나면 선택지가 자동으로 붙는다:
//   Available              -> offer 대사    -> [수락한다] [거절한다]
//   InProgress + 목표 미달  -> progress 대사 -> [돌아가기]
//   InProgress + 목표 달성  -> complete 대사 -> [완료하기] [나중에]
//   Completed              -> 목록에서 숨김
// 선행 퀘스트가 안 끝난 퀘스트도 목록에서 숨긴다(QuestManager.CanAccept).
// dialogue_choice.csv에 손으로 쓴 선택지가 있으면 그게 우선이라 기존 분기 방식도 그대로 동작한다.
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

    [Tooltip("선택지마다 하나씩 매칭되는 버튼 슬롯들. 0번=첫 번째 선택지, 1번=두 번째 선택지... " +
             "여기 꽂아둔 개수보다 선택지가 많으면 0번 슬롯을 복제해서 런타임에 늘린다. " +
             "배치와 간격은 부모의 VerticalLayoutGroup이 처리하므로 좌표는 건드리지 않는다.")]
    [SerializeField] private ChoiceSlot[] _choiceSlots;

    [Header("타이핑 연출")]
    [Tooltip("글자 하나가 찍히는 간격(초). 0이면 연출 없이 바로 전체 출력")]
    [SerializeField] private float _charInterval = 0.03f;
    [Tooltip("몇 글자마다 blip을 낼지. 1이면 매 글자 — 보통 2~3이 자연스럽다")]
    [SerializeField] private int _blipEveryChars = 2;
    [Tooltip("재생마다 흔들 피치 폭(±). 0이면 매번 같은 소리라 기계음처럼 들린다")]
    [SerializeField] private float _blipPitchVariance = 0.12f;
    [SerializeField] private string _blipSoundKey = "ui_dialogue_blip";

    // 선택지 하나 = 화면에 보일 문구 + 골랐을 때 할 일.
    // CSV 선택지와 런타임 생성 선택지(퀘스트 목록/수락/완료)를 같은 형태로 담아야
    // 버튼·숫자키 입력 쪽이 둘을 구분하지 않는다.
    private readonly struct Choice
    {
        public readonly string Text;
        public readonly Action Select;

        public Choice(string text, Action select)
        {
            Text = text;
            Select = select;
        }
    }

    // 퀘스트 대사 중 어느 단계를 띄우고 있는지 — 대사가 끝날 때 붙일 선택지가 이걸로 갈린다
    private enum QuestStep { None, Offer, Progress, Complete }

    // 현재 줄의 선택지 문구가 바뀔 때마다 호출된다 (빈 리스트면 선택지 없음 — 선택지 UI를 숨기면 됨)
    public event Action<IReadOnlyList<string>> OnChoicesChanged;

    private const int HotkeyLimit = 9; // 숫자키 1~9까지만 받는다

    private int _nextId; // 0이면 다음 대사 없음 — Advance 누르면 퀘스트 목록으로 가거나 닫힌다
    private List<Choice> _pendingChoices = new();
    private readonly List<ChoiceSlot> _runtimeSlots = new();
    private PlayerController _player;
    private Coroutine _typingCoroutine;

    private int _questMenuNpcId; // 0이면 대사가 끝날 때 퀘스트 목록을 띄우지 않는다
    private int _menuLineId;     // 목록으로 돌아올 때 다시 띄울 기본 대사 줄
    private int _activeQuestId;  // 지금 대사를 띄우고 있는 퀘스트 (0이면 기본 대사 중)
    private QuestStep _activeStep;

    protected override UIType UiType => UIType.Dialogue;

    // 대화 중에도 뒷화면이 선명하게 보여야 한다.
    protected override bool UseBackdropBlurByDefault => false;

    protected override void Awake()
    {
        base.Awake(); // UIBase의 자기 등록을 반드시 먼저 태워야 한다

        if (_choiceSlots == null) return;

        for (int i = 0; i < _choiceSlots.Length; i++)
            BindSlot(_choiceSlots[i], i);
    }

    private void BindSlot(ChoiceSlot slot, int index)
    {
        if (slot?.button == null) return;

        slot.button.onClick.RemoveAllListeners();
        slot.button.onClick.AddListener(() => SelectChoice(index));
    }

    private void Update()
    {
        if (_pendingChoices.Count == 0 || Keyboard.current == null) return;

        int count = Mathf.Min(_pendingChoices.Count, HotkeyLimit);
        for (int i = 0; i < count; i++)
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
        Show(); // 타이핑 코루틴은 활성 상태에서만 돌 수 있어 내용 설정보다 먼저 띄운다
        SetContent(speakerName, dialogue);

        _nextId = 0;
        ResetQuestFlow();
        SetChoices(new List<Choice>());
        _player = null;
    }

    // 대사 테이블의 특정 id부터 시작 (NPC 상호작용용) — Dialogue 입력맵으로 전환해 F(Advance)를 받는다.
    // showQuestMenu가 켜져 있으면 대사 체인이 끝났을 때 그 대사의 npc_id에 걸린 퀘스트 목록을 띄운다.
    public void OpenById(int dialogueId, PlayerController player, bool showQuestMenu = false)
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

        ResetQuestFlow();
        _questMenuNpcId = showQuestMenu ? line.NpcId : 0;

        ShowLine(line);
    }

    // 이미 열려있는 상태에서 내용만 갱신할 때
    public void SetContent(string speakerName, string dialogue)
    {
        if (_nameText != null) _nameText.text = speakerName;
        if (_dialogueText == null) return;

        StopTyping();
        _dialogueText.text = dialogue;

        if (_charInterval <= 0f || !gameObject.activeInHierarchy)
        {
            _dialogueText.maxVisibleCharacters = int.MaxValue;
            return;
        }

        _typingCoroutine = StartCoroutine(TypeRoutine());
    }

    public bool IsTyping => _typingCoroutine != null;

    // 남은 글자를 한 번에 전부 표시한다 (스킵)
    public void CompleteTyping()
    {
        if (_typingCoroutine == null) return;

        StopTyping();
        _dialogueText.maxVisibleCharacters = int.MaxValue;
    }

    private void StopTyping()
    {
        if (_typingCoroutine == null) return;

        StopCoroutine(_typingCoroutine);
        _typingCoroutine = null;
    }

    private IEnumerator TypeRoutine()
    {
        // maxVisibleCharacters를 늘려가는 방식 — 매 글자 문자열을 새로 만들지 않아 GC가 없다
        _dialogueText.ForceMeshUpdate();
        int total = _dialogueText.textInfo.characterCount;
        _dialogueText.maxVisibleCharacters = 0;

        int sinceBlip = 0;
        for (int i = 0; i < total; i++)
        {
            _dialogueText.maxVisibleCharacters = i + 1;

            // 공백·문장부호에서는 소리를 내지 않는다 — 말소리처럼 들리게 하는 핵심
            char c = _dialogueText.textInfo.characterInfo[i].character;
            if (!char.IsWhiteSpace(c) && !char.IsPunctuation(c) && ++sinceBlip >= _blipEveryChars)
            {
                sinceBlip = 0;
                SoundManager.GetInstance()?.PlaySfxPitched(_blipSoundKey, _blipPitchVariance);
            }

            yield return new WaitForSecondsRealtime(_charInterval);
        }

        _typingCoroutine = null;
    }

    // 외부 선택지 UI가 버튼 클릭 등으로 호출 — index는 현재 선택지 목록(OnChoicesChanged로 받은 것) 기준
    public void SelectChoice(int index)
    {
        if (index < 0 || index >= _pendingChoices.Count) return;

        // 고른 동작이 또 선택지를 띄울 수 있으니 목록을 먼저 비운다 — 안 그러면 같은 선택지를 두 번 누를 수 있다
        Action select = _pendingChoices[index].Select;
        SetChoices(new List<Choice>());
        select?.Invoke();
    }

    private void Advance()
    {
        if (_pendingChoices.Count > 0) return; // 선택지 표시 중엔 F 무시

        // 타이핑 중이면 먼저 전체를 띄운다 — 다음 줄로 넘어가려면 한 번 더 눌러야 한다
        if (IsTyping)
        {
            CompleteTyping();
            return;
        }

        DialogueData next = _nextId > 0 ? DialogueTable.Instance.Get(_nextId) : null;
        if (next != null)
        {
            ShowLine(next);
            return;
        }

        // 퀘스트 대사가 끝났으면 그 퀘스트의 수락/완료 선택지를, 기본 대사가 끝났으면 퀘스트 목록을 띄운다
        if (ShowQuestStepChoices()) return;
        if (ShowQuestMenu()) return;

        Hide();
    }

    // 대사 한 줄을 표시하고, 그 줄에 딸린 선택지 목록을 알린다
    private void ShowLine(DialogueData line)
    {
        _nextId = line.NextId;

        // 기본 대사의 마지막 줄 — 퀘스트 목록으로 돌아올 때마다 이 줄을 다시 띄운다
        if (_activeQuestId == 0 && line.NextId == 0) _menuLineId = line.Id;

        SetContent(line.Speaker, line.Text);
        SetChoices(BuildCsvChoices(line.Id));
    }

    // dialogue_choice.csv에 손으로 써둔 선택지. 수락 불가한 퀘스트로 이어지는 선택지는 빼고 보여준다.
    private List<Choice> BuildCsvChoices(int dialogueId)
    {
        var result = new List<Choice>();
        foreach (DialogueChoiceData data in DialogueChoiceTable.Instance.All
                     .Where(c => c.DialogueId == dialogueId)
                     .OrderBy(c => c.Order))
        {
            if (data.AcceptQuestId > 0 && !QuestManager.CanAccept(data.AcceptQuestId)) continue;

            DialogueChoiceData choice = data; // 클로저 캡처용
            result.Add(new Choice(choice.Text, () => SelectCsvChoice(choice)));
        }
        return result;
    }

    private void SelectCsvChoice(DialogueChoiceData choice)
    {
        if (choice.AcceptQuestId > 0) TryAccept(choice.AcceptQuestId);

        DialogueData next = choice.NextId > 0 ? DialogueTable.Instance.Get(choice.NextId) : null;
        if (next != null)
        {
            ShowLine(next);
            return;
        }

        ReturnToMenu();
    }

    // 이 NPC가 가진 퀘스트를 상태별로 한 줄씩. 완료했거나 선행 조건이 안 찬 퀘스트는 숨긴다.
    private bool ShowQuestMenu()
    {
        _activeQuestId = 0;
        _activeStep = QuestStep.None;
        if (_questMenuNpcId <= 0) return false;

        var choices = new List<Choice>();
        foreach (QuestData quest in QuestTable.Instance.All
                     .Where(q => q.NpcId == _questMenuNpcId)
                     .OrderBy(q => q.Id))
        {
            QuestState state = QuestManager.GetState(quest.Id);
            if (state == QuestState.Completed) continue;

            int questId = quest.Id; // 클로저 캡처용
            if (state == QuestState.Available && !QuestManager.CanAccept(questId)) continue;

            QuestStep step = state == QuestState.Available ? QuestStep.Offer
                : QuestManager.IsGoalMet(questId) ? QuestStep.Complete
                : QuestStep.Progress;
            choices.Add(new Choice(quest.QuestName, () => OpenQuestStep(questId, step)));
        }

        if (choices.Count == 0) return false;
        choices.Add(new Choice("돌아가기", Hide));

        // 퀘스트 대사가 화면에 남아 있으면 목록과 문맥이 어긋난다 — 기본 대사의 마지막 줄로 되돌린다
        DialogueData line = _menuLineId > 0 ? DialogueTable.Instance.Get(_menuLineId) : null;
        if (line != null)
        {
            _nextId = 0;
            SetContent(line.Speaker, line.Text);
        }

        SetChoices(choices);
        return true;
    }

    private void ReturnToMenu()
    {
        if (!ShowQuestMenu()) Hide();
    }

    // 목록에서 퀘스트를 고르면 그 단계의 대사부터 시작한다 — 대사가 끝나면 ShowQuestStepChoices가 이어받는다
    private void OpenQuestStep(int questId, QuestStep step)
    {
        QuestData quest = QuestTable.Instance.Get(questId);
        if (quest == null)
        {
            ReturnToMenu();
            return;
        }

        _activeQuestId = questId;
        _activeStep = step;

        int dialogueId = step switch
        {
            QuestStep.Offer => quest.OfferDialogueId,
            QuestStep.Progress => quest.ProgressDialogueId,
            QuestStep.Complete => quest.CompleteDialogueId,
            _ => 0,
        };

        DialogueData line = dialogueId > 0 ? DialogueTable.Instance.Get(dialogueId) : null;
        if (line != null)
        {
            ShowLine(line);
            return;
        }

        // 대사를 안 걸어둔 퀘스트는 quest.csv의 설명으로 대신한다 — 선택지까지 못 가고 끊기면 안 된다
        _nextId = 0;
        SetContent(quest.NpcName, quest.Description);
        if (!ShowQuestStepChoices()) ReturnToMenu();
    }

    // 퀘스트 대사가 끝났을 때 붙는 선택지 — 수락과 완료는 여기서만 일어난다
    private bool ShowQuestStepChoices()
    {
        if (_activeQuestId <= 0 || _activeStep == QuestStep.None) return false;

        int questId = _activeQuestId; // 클로저 캡처용
        var choices = new List<Choice>();
        switch (_activeStep)
        {
            case QuestStep.Offer:
                choices.Add(new Choice("수락한다", () => { TryAccept(questId); ReturnToMenu(); }));
                choices.Add(new Choice("거절한다", ReturnToMenu));
                break;

            case QuestStep.Complete:
                choices.Add(new Choice("완료하기", () => { TryComplete(questId); ReturnToMenu(); }));
                choices.Add(new Choice("나중에", ReturnToMenu));
                break;

            default:
                choices.Add(new Choice("돌아가기", ReturnToMenu));
                break;
        }

        _activeStep = QuestStep.None; // 같은 대사에 선택지가 두 번 붙지 않게
        SetChoices(choices);
        return true;
    }

    // 퀘스트 상태는 호스트가 확정한다 — 로컬에 반영됐을 때만 RoomSync로 전파한다
    private static void TryAccept(int questId)
    {
        if (QuestManager.Accept(questId)) RoomSync.QuestAccept(questId);
    }

    private static void TryComplete(int questId)
    {
        if (!QuestManager.Complete(questId)) return;

        RoomSync.QuestComplete(questId);
        // 보상은 완료를 누른 이 클라이언트만 받는다 — 인벤토리가 꽉 차면 지급 대기열에 남는다
        QuestManager.ReceiveReward(questId);
        QuestManager.FlushLocalItems();
    }

    private void SetChoices(List<Choice> choices)
    {
        _pendingChoices = choices;

        int slotCount = Mathf.Max(choices.Count, (_choiceSlots?.Length ?? 0) + _runtimeSlots.Count);
        for (int i = 0; i < slotCount; i++)
        {
            ChoiceSlot slot = GetSlot(i);
            if (slot?.button == null) continue;

            bool hasChoice = i < choices.Count;
            slot.button.gameObject.SetActive(hasChoice);
            if (hasChoice && slot.text != null) slot.text.text = $"{i + 1}. {choices[i].Text}";
        }

        OnChoicesChanged?.Invoke(choices.Select(c => c.Text).ToList());
    }

    // 인스펙터에 꽂아둔 슬롯보다 선택지가 많으면 0번 슬롯을 복제해 늘린다 —
    // 퀘스트 개수는 quest.csv에서 늘어나는데 프리팹을 매번 다시 만질 수는 없다.
    // 배치는 부모(SelectTexts)의 VerticalLayoutGroup이 한다 — 복제본은 마지막 자식이라 목록 아래쪽에 붙는다.
    private ChoiceSlot GetSlot(int index)
    {
        int fixedCount = _choiceSlots?.Length ?? 0;
        if (index < fixedCount) return _choiceSlots[index];

        int extra = index - fixedCount;
        while (_runtimeSlots.Count <= extra)
        {
            ChoiceSlot created = CreateSlot(fixedCount + _runtimeSlots.Count);
            if (created == null) return null;

            _runtimeSlots.Add(created);
        }
        return _runtimeSlots[extra];
    }

    private ChoiceSlot CreateSlot(int index)
    {
        ChoiceSlot template = _choiceSlots != null && _choiceSlots.Length > 0 ? _choiceSlots[0] : null;
        if (template?.button == null) return null;

        GameObject clone = Instantiate(template.button.gameObject, template.button.transform.parent);
        clone.name = $"{template.button.name} ({index})";

        var slot = new ChoiceSlot
        {
            button = clone.GetComponent<Button>(),
            text = clone.GetComponentInChildren<TextMeshProUGUI>(true),
        };
        BindSlot(slot, index);
        return slot;
    }

    private void ResetQuestFlow()
    {
        _questMenuNpcId = 0;
        _menuLineId = 0;
        _activeQuestId = 0;
        _activeStep = QuestStep.None;
    }

    protected override void OnHide()
    {
        StopTyping();
        ResetQuestFlow();
        SetChoices(new List<Choice>());

        if (_player != null)
        {
            _player.InputHandler.DialogueAdvance -= Advance;
            _player.InputHandler.SwitchToGameplayMapNextFrame();
            _player = null;
        }
    }
}
