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
// 네트워크 동기화: Accept/Submit/Complete 전부 구현됨 (RoomSync.QuestAccept/QuestSubmit/QuestComplete,
// PacketHandler.Quest.cs G/H 양쪽).
//   - Accept/Complete: "상태를 X로 맞춘다"라 멱등이다 - 트리거 쪽이 로컬에 먼저 낙관적으로 적용한 뒤
//     RoomSync로 전파한다(ShelterManager.TryUpgrade와 동일 패턴). 이미 그 상태면 조용히 무시하므로
//     호스트의 재확인 브로드캐스트가 되돌아와도 안전하다.
//   - Submit(AddSubmitted): "누적값에 더한다"라 멱등이 아니다 - 게스트는 로컬에 낙관적으로 적용하지
//     않고 요청만 보낸다. 실제 반영은 호스트가 AddSubmitted를 호출한 뒤 보내주는 H_QuestSubmit을
//     받을 때 한다 (안 그러면 자기 요청이 되돌아올 때 이중 집계됨). QuestDescriptionUI.OnClickAction 참고.
//   - late-join 시 SendWorldStateToGuest에 퀘스트 스냅샷(현재 상태 전체) 전송 아직 없음 - 새로 들어온
//     게스트는 그 전에 있었던 Accept/Submit/Complete를 못 받는다.
//   - 호스트는 게스트가 보낸 Submit amount를 그대로 믿는다(검증 없음) - G_Move 등 다른 자기보고 패킷과
//     동일한 신뢰 수준. 실제로 그 아이템을 갖고 있었는지는 확인 안 함.
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
        // 이미 수락(이상 진행)된 퀘스트면 무시 - 호스트의 재확인 브로드캐스트가 자기 자신에게
        // 되돌아오거나, 같은 대화를 다시 걸었을 때 제출 기록이 지워지는 걸 막는다.
        if (GetState(questId) != QuestState.Available) return;

        ClearSubmissions(questId); // 혹시 남아있던 이전 기록 정리 (정상 경로에선 보통 비어있음)
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
