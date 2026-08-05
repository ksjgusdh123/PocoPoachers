using System;
using System.Collections.Generic;

public enum QuestState
{
    Available,
    InProgress,
    Completed,
}

// 퀘스트 진행 상태 저장소 — 호스트 권위, 파티(호스트+게스트) 전체가 공유하는 단일 상태.
// WorldEquipmentManager와 동일한 패턴: 호스트 메모리에 Dictionary로 들고 있다가 SaveManager가 영속화.
//
// 정의(이름/NPC/설명/목표·보상 아이템 등 고정 데이터)는 quest.csv(QuestTable)가 담당하고, 여기는
// "어떤 퀘스트가 지금 Available/InProgress/Completed 중 뭔지"와 "퀘스트+아이템별로 몇 개 제출했는지"만 가진다.
// 목표 하나에 아이템이 여러 종류(QuestData.GoalItems)일 수 있어서 제출량도 questId 하나가 아니라
// (questId, itemId) 조합으로 따로 센다. 목표 수량과 비교해서 다 채웠는지 판단하는 건 호출 쪽
// (QuestDescriptionUI) 몫이다 — QuestManager는 QuestTable을 몰라도 되게 의도적으로 분리했다.
//
// 네트워크 동기화는 아직 미구현이다. 계획:
//   - G_QuestAccept / G_QuestSubmit(게스트→호스트, 신뢰): 호스트가 Accept()/AddSubmitted() 호출
//   - H_QuestStateChanged / H_QuestSubmitted(호스트→게스트 브로드캐스트, 신뢰): 각 이벤트 구독해서 보내면 됨
//   - late-join 시 SendWorldStateToGuest에 퀘스트 스냅샷 추가 필요
// 위 패킷들이 생기기 전까지 게스트 클라이언트에서 Accept/Submit/Complete를 호출하면 로컬에서만 바뀌고
// 호스트에는 전파되지 않는다 — 반드시 호스트 측 코드(패킷 핸들러 등)에서만 호출할 것.
public static class QuestManager
{
    private static readonly Dictionary<int, QuestState> _progress = new();

    // (questId, itemId) -> 지금까지 제출한 개수 (인벤토리 보유량이 아니라 누적 제출량)
    private static readonly Dictionary<(int questId, int itemId), int> _submitted = new();

    // 상태가 바뀔 때마다 발행 — QuestListUI 갱신 + (구현되면) 호스트의 H_QuestStateChanged 브로드캐스트가 여기 구독
    public static event Action<int, QuestState> OnQuestStateChanged;

    // 제출 수량이 바뀔 때마다 발행(questId, itemId, 해당 아이템 누적 제출량) — 목표 표시/액션 버튼이 구독
    public static event Action<int, int, int> OnSubmittedChanged;

    // 기록이 없으면 Available(아직 수락 전)로 취급 — 모든 퀘스트가 기본적으로 열려있다는 전제
    public static QuestState GetState(int questId) =>
        _progress.TryGetValue(questId, out var state) ? state : QuestState.Available;

    public static int GetSubmittedCount(int questId, int itemId) =>
        _submitted.TryGetValue((questId, itemId), out var count) ? count : 0;

    public static void Accept(int questId)
    {
        ClearSubmissions(questId); // 재수락 대비 - 새로 시작하면 제출 기록도 초기화
        SetState(questId, QuestState.InProgress);
    }

    // itemId에 amount만큼 제출량을 더한다 (아이템을 실제로 인벤토리에서 빼는 건 호출 쪽 책임 - 여긴 카운트만)
    public static void AddSubmitted(int questId, int itemId, int amount)
    {
        if (amount <= 0) return;
        int newCount = GetSubmittedCount(questId, itemId) + amount;
        _submitted[(questId, itemId)] = newCount;
        OnSubmittedChanged?.Invoke(questId, itemId, newCount);
    }

    public static void Complete(int questId) => SetState(questId, QuestState.Completed);

    public static void SetState(int questId, QuestState state)
    {
        if (_progress.TryGetValue(questId, out var current) && current == state) return;
        _progress[questId] = state;
        OnQuestStateChanged?.Invoke(questId, state);
    }

    private static void ClearSubmissions(int questId)
    {
        var keysToRemove = new List<(int, int)>();
        foreach (var key in _submitted.Keys)
            if (key.questId == questId) keysToRemove.Add(key);
        foreach (var key in keysToRemove)
            _submitted.Remove(key);
    }

    public static void Clear()
    {
        _progress.Clear();
        _submitted.Clear();
    }

    // ---- 영속화 (SaveManager.SaveQuestState/LoadQuestState) ----

    public static SaveData Export()
    {
        var data = new SaveData();
        foreach (var kv in _progress)
            data.entries.Add(new Entry { questId = kv.Key, state = (int)kv.Value });
        foreach (var kv in _submitted)
            data.submissions.Add(new SubmissionEntry { questId = kv.Key.questId, itemId = kv.Key.itemId, count = kv.Value });
        return data;
    }

    public static void Import(SaveData data)
    {
        _progress.Clear();
        _submitted.Clear();
        if (data == null) return;

        if (data.entries != null)
            foreach (var e in data.entries)
                _progress[e.questId] = (QuestState)e.state;

        if (data.submissions != null)
            foreach (var s in data.submissions)
                _submitted[(s.questId, s.itemId)] = s.count;
    }

    [Serializable]
    public class SaveData
    {
        public List<Entry> entries = new();
        public List<SubmissionEntry> submissions = new();
    }

    [Serializable]
    public class Entry
    {
        public int questId;
        public int state; // (int)QuestState
    }

    [Serializable]
    public class SubmissionEntry
    {
        public int questId;
        public int itemId;
        public int count;
    }
}
