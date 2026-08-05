using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 좌측 퀘스트 목록(상태 탭 + 스크롤 목록). 항목을 고르면 QuestDescriptionUI에 표시한다.
// 정의(이름/설명/보상)는 quest.csv → QuestTable, 진행 상태(수락/진행중/완료)는 QuestManager(파티 공유)에서 가져온다.
public class QuestListUI : MonoBehaviour
{
    [Header("Status Tabs (수락 가능 / 진행 중 / 완료 버튼 순서와 맞춰 연결)")]
    [SerializeField] private Button[] _filterButtons;
    [SerializeField] private QuestState[] _filterStates;

    [Header("Entry List")]
    [SerializeField] private QuestListEntryUI _entryPrefab;
    [SerializeField] private Transform _entryParent;

    [Header("Description")]
    [SerializeField] private QuestDescriptionUI _descriptionPanel;

    private readonly List<QuestListEntryUI> _entries = new();
    private QuestState _selectedFilter;
    private QuestData _selectedQuest;

    private void Awake()
    {
        for (int i = 0; i < _filterButtons.Length; i++)
        {
            if (_filterButtons[i] == null || i >= _filterStates.Length) continue;
            QuestState state = _filterStates[i];
            _filterButtons[i].onClick.AddListener(() => SelectFilter(state));
        }
    }

    private void OnEnable()
    {
        QuestManager.OnQuestStateChanged += HandleQuestStateChanged;
    }

    private void OnDisable()
    {
        QuestManager.OnQuestStateChanged -= HandleQuestStateChanged;
    }

    private void Start()
    {
        SelectFilter(_filterStates.Length > 0 ? _filterStates[0] : QuestState.Available);
    }

    // 다른 경로(패킷 핸들러 등)로 퀘스트 상태가 바뀌어도 지금 보고 있는 탭이면 바로 반영
    private void HandleQuestStateChanged(int questId, QuestState state)
    {
        RefreshList();
    }

    public void SelectFilter(QuestState state)
    {
        _selectedFilter = state;
        _selectedQuest = null;
        _descriptionPanel?.Clear();
        RefreshFilterVisual();
        RefreshList();
    }

    private void RefreshFilterVisual()
    {
        for (int i = 0; i < _filterButtons.Length; i++)
        {
            if (_filterButtons[i] == null || i >= _filterStates.Length) continue;

            bool selected = _filterStates[i] == _selectedFilter;
            var label = _filterButtons[i].GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.color = selected ? UITheme.InkPrimary : UITheme.InkSecondary;
        }
    }

    // 엔트리는 파괴하지 않고 재사용한다(CraftingTableUI와 동일한 방식) - 탭 전환마다 Destroy/Instantiate가
    // 반복되면 GC 스파이크가 생긴다.
    private void RefreshList()
    {
        int used = 0;

        foreach (var quest in QuestTable.Instance.All)
        {
            if (QuestManager.GetState(quest.Id) != _selectedFilter) continue;

            if (used == _entries.Count)
                _entries.Add(Instantiate(_entryPrefab, _entryParent));

            var entry = _entries[used];
            used++;
            if (entry == null) continue;

            if (!entry.gameObject.activeSelf) entry.gameObject.SetActive(true);
            entry.transform.SetSiblingIndex(used - 1);
            entry.Setup(quest, OnQuestSelected);
            entry.SetSelected(quest == _selectedQuest);
        }

        for (int i = used; i < _entries.Count; i++)
        {
            if (_entries[i] != null && _entries[i].gameObject.activeSelf)
                _entries[i].gameObject.SetActive(false);
        }
    }

    private void OnQuestSelected(QuestData quest, QuestListEntryUI entryUI)
    {
        _selectedQuest = quest;
        _descriptionPanel?.SetQuest(quest);

        foreach (var entry in _entries)
            entry.SetSelected(entry == entryUI);
    }
}
