using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 좌측 퀘스트 목록(상태 탭 + 스크롤 목록). 항목을 고르면 QuestDescriptionUI에 표시한다.
// 데이터는 아직 QuestTable/QuestManager가 없어 인스펙터에 채우는 임시 테스트 데이터를 쓴다 —
// 나중에 실제 데이터 소스가 생기면 _testQuests 대신 그쪽에서 목록을 받아오도록 바꾸면 된다.
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

    [Header("Test Data (임시 - 추후 실제 데이터 소스로 교체 예정)")]
    [SerializeField] private List<QuestData> _testQuests = new();

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

    private void Start()
    {
        SelectFilter(_filterStates.Length > 0 ? _filterStates[0] : QuestState.Available);
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

        foreach (var quest in _testQuests)
        {
            if (quest.State != _selectedFilter) continue;

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
