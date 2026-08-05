using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 퀘스트 상세 표시 패널. QuestListUI가 목록에서 항목을 고르면 SetQuest로 내용을 채운다.
// 목표/보상은 아이템이 여러 종류일 수 있어(QuestData.GoalItems/RewardItems) 한 줄씩 묶어서 표시한다.
// 액션 버튼 하나가 상태에 따라 라벨/동작을 바꾼다:
//   Available              -> "수락하기" (클릭 시 QuestManager.Accept)
//   InProgress + 제출 미달   -> "제출하기" (클릭 시 목표 아이템 전부를 인벤토리에서 꺼내 QuestManager.AddSubmitted)
//   InProgress + 전부 제출됨 -> "완료하기" (클릭 시 QuestManager.Complete)
//   Completed               -> 버튼 숨김
public class QuestDescriptionUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _questNameText;
    [SerializeField] private TextMeshProUGUI _questNpcNameText;
    [SerializeField] private TextMeshProUGUI _questDescriptionText;
    [SerializeField] private TextMeshProUGUI _questGoalText;
    [SerializeField] private TextMeshProUGUI _questRewardText;

    [Header("Action Button (수락하기 / 제출하기 / 완료하기 공용)")]
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

        if (_questNameText != null) _questNameText.text = data.QuestName;
        if (_questNpcNameText != null) _questNpcNameText.text = data.NpcName;
        if (_questDescriptionText != null) _questDescriptionText.text = data.Description;
        if (_questRewardText != null) _questRewardText.text = FormatItemLines(data.RewardItems);

        RefreshGoalText();
        RefreshActionButton();
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
        if (_currentQuest == null || _currentQuest.Id != questId) return;
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
            case QuestState.Available:
                _actionButton.gameObject.SetActive(true);
                _actionButton.interactable = true;
                if (_actionButtonText != null) _actionButtonText.text = "수락하기";
                break;

            case QuestState.InProgress:
                bool goalMet = IsGoalFullyMet(_currentQuest);
                _actionButton.gameObject.SetActive(true);
                _actionButton.interactable = true;
                if (_actionButtonText != null) _actionButtonText.text = goalMet ? "완료하기" : "제출하기";
                break;

            case QuestState.Completed:
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

    // CheatConsole.FindLocalPlayer()와 동일한 방식 - 씬에 여러 PlayerController(원격 포함)가 있을 수 있어
    // 실제 입력이 활성화된 것을 로컬로 판단한다
    private static Inventory FindLocalInventory()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        PlayerController fallback = null;

        foreach (var player in players)
        {
            fallback ??= player;
            var input = player.GetComponent<PlayerInputHandler>();
            if (input != null && input.isActiveAndEnabled)
                return player.PlayerInventory;
        }

        return fallback?.PlayerInventory;
    }

    private void OnClickAction()
    {
        if (_currentQuest == null) return;

        QuestState state = QuestManager.GetState(_currentQuest.Id);
        if (state == QuestState.Available)
        {
            QuestManager.Accept(_currentQuest.Id);
            return;
        }

        if (state != QuestState.InProgress) return;

        if (IsGoalFullyMet(_currentQuest))
        {
            QuestManager.Complete(_currentQuest.Id);
            return;
        }

        // 아직 다 안 찼으면 "제출하기" - 목표 아이템마다 들고 있는 만큼(최대 남은 필요량까지) 인벤토리에서 꺼내 제출량에 더한다
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
            if (removed > 0) QuestManager.AddSubmitted(_currentQuest.Id, itemId, removed);
        }
    }
}
