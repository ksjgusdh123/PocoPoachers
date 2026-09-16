using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 퀘스트 상세 표시 패널. QuestListUI가 목록에서 항목을 고르면 SetQuest로 내용을 채운다.
// 목표/보상은 아이템이 여러 종류일 수 있어(QuestData.GoalItems/RewardItems) 한 줄씩 묶어서 표시한다.
// 수락은 이 UI가 아니라 NPC 퀘스트 대화(DialogueUI)에서만 한다 -
// 그래서 Available 상태에서는 액션 버튼이 아예 안 뜬다.
// 액션 버튼 하나가 상태에 따라 라벨/동작을 바꾼다:
//   Available              -> 버튼 숨김 (수락은 대화로만)
//   InProgress + 제출 미달   -> "제출하기" (클릭 시 목표 아이템 전부를 인벤토리에서 꺼내 호스트에 제출 요청)
//   InProgress + 전부 제출됨 -> "제출하기" 비활성화 (완료는 NPC 대화에서 처리)
//   Completed               -> 버튼 숨김
public class QuestDescriptionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _questNameText;
    [SerializeField] private TextMeshProUGUI _questNpcNameText;
    [SerializeField] private TextMeshProUGUI _questDescriptionText;
    [SerializeField] private TextMeshProUGUI _questGoalText;
    [SerializeField] private TextMeshProUGUI _questRewardText;

    [Header("Action Button (제출하기 전용 - 수락과 완료는 NPC 대화)")]
    [SerializeField] private Button _actionButton;
    [SerializeField] private TextMeshProUGUI _actionButtonText;

    private QuestData _currentQuest;

    private void Awake()
    {
        _actionButton?.onClick.AddListener(OnClickAction);
    }

    private void OnEnable()
    {
        QuestManager.OnQuestStateChanged += HandleQuestChanged;
        QuestManager.OnSubmittedChanged += HandleSubmittedChanged;
    }

    private void OnDisable()
    {
        QuestManager.OnQuestStateChanged -= HandleQuestChanged;
        QuestManager.OnSubmittedChanged -= HandleSubmittedChanged;
    }

    public void SetQuest(QuestData data)
    {
        if (data == null)
        {
            Clear();
            return;
        }

        _currentQuest = data;

        if (_questNameText != null) _questNameText.text = $"[{QuestManager.GetModeLabel(data.Id)}] {data.QuestName}";
        if (_questNpcNameText != null) _questNpcNameText.text = data.NpcName;
        RefreshDescription();
        if (_questRewardText != null) _questRewardText.text = FormatItemLines(data.RewardItems);

        RefreshGoalText();
        RefreshActionButton();
    }

    private void RefreshDescription()
    {
        if (_questDescriptionText == null || _currentQuest == null) return;
        var text = new StringBuilder(_currentQuest.Description);
        if (!string.IsNullOrWhiteSpace(_currentQuest.PrerequisiteQuestIds))
        {
            text.Append("\n\n선행 퀘스트 (모두 완료 필요)");
            foreach (string token in _currentQuest.PrerequisiteQuestIds.Split(';'))
            {
                if (!int.TryParse(token.Trim(), out int id)) continue;
                var prerequisite = QuestTable.Instance.Get(id);
                string status = QuestManager.GetState(id) == QuestState.Completed ? "완료" : "미완료";
                text.Append($"\n- {prerequisite?.QuestName ?? token} [{status}]");
            }
        }
        _questDescriptionText.text = text.ToString();
    }

    public void Clear()
    {
        _currentQuest = null;

        if (_questNameText != null) _questNameText.text = "-";
        if (_questNpcNameText != null) _questNpcNameText.text = "-";
        if (_questDescriptionText != null) _questDescriptionText.text = "-";
        if (_questGoalText != null) _questGoalText.text = "-";
        if (_questRewardText != null) _questRewardText.text = "-";

        _actionButton?.gameObject.SetActive(false);
    }

    private void HandleQuestChanged(int questId, QuestState state)
    {
        if (_currentQuest == null) return;
        RefreshDescription();
        RefreshActionButton();
    }

    private void HandleSubmittedChanged(int questId, int itemId, int submitted)
    {
        if (_currentQuest == null || _currentQuest.Id != questId) return;
        RefreshGoalText();
        RefreshActionButton();
    }

    // 목표 아이템마다 "이름 (제출개수 / 목표개수)"를 한 줄씩 - 제출개수는 인벤토리 보유량이 아니라 QuestManager 누적치
    private void RefreshGoalText()
    {
        if (_questGoalText == null || _currentQuest == null) return;

        var goals = _currentQuest.GoalItems;
        if (goals.Count == 0)
        {
            _questGoalText.text = "-";
            return;
        }

        var sb = new StringBuilder();
        for (int i = 0; i < goals.Count; i++)
        {
            var (itemId, target) = goals[i];
            var item = ItemTable.Instance.Get(itemId);
            if (item == null) continue;

            string name = LocalizationManager.GetInstance().GetString(item.name);
            int submitted = QuestManager.GetSubmittedCount(_currentQuest.Id, itemId);
            if (sb.Length > 0) sb.Append('\n');
            sb.Append($"{name} ({submitted} / {target})");
        }
        _questGoalText.text = sb.Length > 0 ? sb.ToString() : "-";
    }

    private void RefreshActionButton()
    {
        if (_actionButton == null) return;

        if (_currentQuest == null)
        {
            _actionButton.gameObject.SetActive(false);
            return;
        }

        QuestState state = QuestManager.GetState(_currentQuest.Id);

        switch (state)
        {
            case QuestState.Available: // 수락은 대화로만 - 여기선 버튼 없음
            case QuestState.Completed:
                _actionButton.gameObject.SetActive(false);
                break;

            case QuestState.InProgress:
                bool goalMet = IsGoalFullyMet(_currentQuest);
                _actionButton.gameObject.SetActive(true);
                _actionButton.interactable = !goalMet;
                if (_actionButtonText != null) _actionButtonText.text = "제출하기";
                break;

            default:
                _actionButton.gameObject.SetActive(false);
                break;
        }
    }

    private static bool IsGoalFullyMet(QuestData quest)
    {
        foreach (var (itemId, target) in quest.GoalItems)
            if (QuestManager.GetSubmittedCount(quest.Id, itemId) < target) return false;
        return true;
    }

    // 보상은 아직 갖고 있는 게 아니라 받을 값이라 개수 표시만 - 아이템마다 "이름 x개수"를 한 줄씩
    private static string FormatItemLines(IReadOnlyList<(int itemId, int count)> items)
    {
        if (items.Count == 0) return "-";

        var sb = new StringBuilder();
        foreach (var (itemId, count) in items)
        {
            var item = ItemTable.Instance.Get(itemId);
            if (item == null) continue;

            string name = LocalizationManager.GetInstance().GetString(item.name);
            if (sb.Length > 0) sb.Append('\n');
            sb.Append($"{name} x{count}");
        }
        return sb.Length > 0 ? sb.ToString() : "-";
    }

    // QuestManager가 보유 중인 지급 대기량을 로컬 인벤토리에 넣고 실제 지급된 수량을 돌려준다.
    public static int TryGiveItem(int itemId, int count)
    {
        if (count <= 0) return 0;

        var item = ItemTable.Instance.Get(itemId);
        if (item == null) return count;

        var inventory = FindLocalInventory();
        return inventory != null ? inventory.AddItem(item, count) : 0;
    }

    // CheatConsole.FindLocalPlayer()와 동일한 방식 - 씬에 여러 PlayerController(원격 포함)가 있을 수 있어
    // 실제 입력이 활성화된 것을 로컬로 판단한다
    private static Inventory FindLocalInventory()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var player in players)
        {
            var input = player.GetComponent<PlayerInputHandler>();
            if (input != null && input.isActiveAndEnabled)
                return player.PlayerInventory;
        }

        return null;
    }

    private void OnClickAction()
    {
        if (_currentQuest == null) return;

        // 수락은 대화로만 하므로 여기선 InProgress 상태의 제출만 처리한다
        if (QuestManager.GetState(_currentQuest.Id) != QuestState.InProgress) return;

        // 목표를 채운 뒤에는 NPC의 완료 대화를 통해서만 완료를 요청한다.
        if (IsGoalFullyMet(_currentQuest)) return;

        // 제출 아이템을 먼저 차감하고 호스트 승인 후 초과/거절된 수량을 반환받는다.
        var inventory = FindLocalInventory();
        if (inventory == null) return;

        foreach (var (itemId, target) in _currentQuest.GoalItems)
        {
            int remaining = target - QuestManager.GetSubmittedCount(_currentQuest.Id, itemId);
            if (remaining <= 0) continue;

            var item = ItemTable.Instance.Get(itemId);
            if (item == null) continue;

            int held = inventory.GetItemCount(item);
            int toSubmit = Mathf.Min(remaining, held);
            if (toSubmit <= 0) continue;

            int removed = inventory.RemoveItem(item, toSubmit);
            if (removed <= 0) continue;

            RoomSync.QuestSubmit(_currentQuest.Id, itemId, removed);
        }
    }
}
