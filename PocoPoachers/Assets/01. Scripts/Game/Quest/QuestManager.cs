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
// 정의(이름/NPC/설명/보상 등 고정 데이터)는 quest.csv(QuestTable)가 담당하고, 여기는
// "어떤 퀘스트가 지금 Available/InProgress/Completed 중 뭔지"만 가진다.
//
// 네트워크 동기화는 아직 미구현이다. 계획:
//   - G_QuestAccept(게스트→호스트, 신뢰): 호스트가 Accept() 호출
//   - 진행/완료 판정은 호스트가 이미 권위를 가진 이벤트(아이템 획득 H_ItemGainResult, 적 처치 H_EnemyDie 등)에
//     직접 훅을 걸어 호스트가 스스로 Complete()를 호출하는 쪽이 자기보고보다 검증이 쉽다
//   - H_QuestStateChanged(호스트→게스트 브로드캐스트, 신뢰): OnQuestStateChanged 구독해서 보내면 됨
//   - late-join 시 SendWorldStateToGuest에 퀘스트 스냅샷 추가 필요
// 위 패킷들이 생기기 전까지 게스트 클라이언트에서 Accept/Complete를 호출하면 로컬에서만 바뀌고
// 호스트에는 전파되지 않는다 — 반드시 호스트 측 코드(패킷 핸들러 등)에서만 호출할 것.
public static class QuestManager
{
    private static readonly Dictionary<int, QuestState> _progress = new();

    // 상태가 바뀔 때마다 발행 — QuestListUI 갱신 + (구현되면) 호스트의 H_QuestStateChanged 브로드캐스트가 여기 구독
    public static event Action<int, QuestState> OnQuestStateChanged;

    // 기록이 없으면 Available(아직 수락 전)로 취급 — 모든 퀘스트가 기본적으로 열려있다는 전제
    public static QuestState GetState(int questId) =>
        _progress.TryGetValue(questId, out var state) ? state : QuestState.Available;

    public static void Accept(int questId) => SetState(questId, QuestState.InProgress);

    public static void Complete(int questId) => SetState(questId, QuestState.Completed);

    public static void SetState(int questId, QuestState state)
    {
        if (_progress.TryGetValue(questId, out var current) && current == state) return;
        _progress[questId] = state;
        OnQuestStateChanged?.Invoke(questId, state);
    }

    public static void Clear() => _progress.Clear();

    // ---- 영속화 (SaveManager.SaveQuestState/LoadQuestState) ----

    public static SaveData Export()
    {
        var data = new SaveData();
        foreach (var kv in _progress)
            data.entries.Add(new Entry { questId = kv.Key, state = (int)kv.Value });
        return data;
    }

    public static void Import(SaveData data)
    {
        _progress.Clear();
        if (data?.entries == null) return;

        foreach (var e in data.entries)
            _progress[e.questId] = (QuestState)e.state;
    }

    [Serializable]
    public class SaveData
    {
        public List<Entry> entries = new();
    }

    [Serializable]
    public class Entry
    {
        public int questId;
        public int state; // (int)QuestState
    }
}
