using System;
using System.Collections.Generic;

public enum QuestState { Available, InProgress, Completed }

// 호스트는 공유 상태(owner=0)와 플레이어별 개인 상태를 관리한다.
// 게스트에는 공유 상태와 본인 상태의 읽기용 사본만 전달한다. 개인 상태의 디스크 저장은 별도 작업이다.
public static class QuestManager
{
    private sealed class Progress
    {
        public QuestState State;
        public long Revision;
        public readonly Dictionary<int, int> Submitted = new();
    }

    private static readonly Dictionary<(int owner, int quest), Progress> _progress = new();
    private static readonly Dictionary<long, (int quest, int item, int count)> _pendingSubmissions = new();
    private static readonly Dictionary<(int player, long request), H_QuestSubmitT> _submissionResults = new();
    private static readonly HashSet<int> _receivedRewards = new();
    private static readonly List<(int item, int count)> _pendingItems = new();
    private static long _nextRequest;

    public static int LocalPlayerId => RoomSync.MyPlayerId != 0 ? RoomSync.MyPlayerId : -1;
    public static event Action<int, QuestState> OnQuestStateChanged;
    public static event Action<int, int, int> OnSubmittedChanged;

    public static bool IsPersonal(int questId) => QuestTable.Instance.Get(questId)?.ProgressMode == ProgressMode.Personal;
    public static bool IsShared(int questId) => QuestTable.Instance.Get(questId)?.ProgressMode == ProgressMode.Shared;
    public static string GetModeLabel(int questId) => IsPersonal(questId) ? "개인" : "공유";
    private static int Owner(int questId, int? player) => IsPersonal(questId) ? player ?? LocalPlayerId : 0;
    private static Progress Find(int questId, int? player = null) =>
        _progress.TryGetValue((Owner(questId, player), questId), out var progress) ? progress : null;
    private static Progress Ensure(int questId, int? player)
    {
        var key = (Owner(questId, player), questId);
        if (!_progress.TryGetValue(key, out var progress)) _progress[key] = progress = new Progress();
        return progress;
    }
    private static void Notify(int questId, int owner, Progress progress)
    {
        if (owner != 0 && owner != LocalPlayerId) return;
        OnQuestStateChanged?.Invoke(questId, progress.State);
        foreach (var item in progress.Submitted) OnSubmittedChanged?.Invoke(questId, item.Key, item.Value);
    }
    public static QuestState GetState(int questId, int? player = null) => Find(questId, player)?.State ?? QuestState.Available;
    public static int GetSubmittedCount(int questId, int itemId, int? player = null) =>
        Find(questId, player)?.Submitted.TryGetValue(itemId, out int count) == true ? count : 0;

    public static bool CanAccept(int questId, int? player = null)
    {
        var quest = QuestTable.Instance.Get(questId);
        if (quest == null || GetState(questId, player) != QuestState.Available) return false;
        if (string.IsNullOrWhiteSpace(quest.PrerequisiteQuestIds)) return true;
        foreach (string token in quest.PrerequisiteQuestIds.Split(';'))
            if (!int.TryParse(token.Trim(), out int id) || id == questId || QuestTable.Instance.Get(id) == null
                || GetState(id, player) != QuestState.Completed) return false;
        return true;
    }
    public static bool Accept(int questId, int? player = null)
    {
        if (!RoomManager.IsHost || !CanAccept(questId, player)) return false;
        var progress = Ensure(questId, player);
        progress.Submitted.Clear();
        progress.State = QuestState.InProgress;
        progress.Revision++;
        Notify(questId, Owner(questId, player), progress);
        return true;
    }
    public static int AddSubmitted(int questId, int itemId, int amount, int? player = null)
    {
        if (!RoomManager.IsHost || amount <= 0 || GetState(questId, player) != QuestState.InProgress) return 0;
        var quest = QuestTable.Instance.Get(questId);
        if (quest == null) return 0;
        int target = 0;
        foreach (var goal in quest.GoalItems) if (goal.itemId == itemId) target = goal.count;
        int current = GetSubmittedCount(questId, itemId, player);
        int accepted = Math.Min(amount, Math.Max(0, target - current));
        if (accepted == 0) return 0;
        var progress = Ensure(questId, player);
        progress.Submitted[itemId] = current + accepted;
        progress.Revision++;
        Notify(questId, Owner(questId, player), progress);
        return accepted;
    }
    public static bool Complete(int questId, int? player = null)
    {
        if (!RoomManager.IsHost || GetState(questId, player) != QuestState.InProgress) return false;
        var quest = QuestTable.Instance.Get(questId);
        if (quest == null) return false;
        foreach (var goal in quest.GoalItems)
            if (GetSubmittedCount(questId, goal.itemId, player) < goal.count) return false;
        var progress = Ensure(questId, player);
        progress.State = QuestState.Completed;
        progress.Revision++;
        Notify(questId, Owner(questId, player), progress);
        return true;
    }

    public static H_QuestAcceptT Snapshot(int questId, int player)
    {
        var progress = Find(questId, player);
        var result = new H_QuestAcceptT { QuestId = questId, PlayerId = Owner(questId, player),
            State = (int)(progress?.State ?? QuestState.Available), Revision = progress?.Revision ?? 0,
            ItemIds = new List<int>(), ItemCounts = new List<int>() };
        if (progress != null) foreach (var item in progress.Submitted) { result.ItemIds.Add(item.Key); result.ItemCounts.Add(item.Value); }
        return result;
    }
    public static void ApplySnapshot(H_QuestAcceptT snapshot)
    {
        if (RoomManager.IsHost || QuestTable.Instance.Get(snapshot.QuestId) == null
            || snapshot.PlayerId != Owner(snapshot.QuestId, LocalPlayerId)
            || snapshot.State < 0 || snapshot.State > (int)QuestState.Completed) return;
        var progress = Ensure(snapshot.QuestId, LocalPlayerId);
        if (snapshot.Revision < progress.Revision) return;
        progress.State = (QuestState)snapshot.State;
        progress.Revision = snapshot.Revision;
        progress.Submitted.Clear();
        for (int i = 0; i < snapshot.ItemIds.Count && i < snapshot.ItemCounts.Count; i++)
            progress.Submitted[snapshot.ItemIds[i]] = snapshot.ItemCounts[i];
        Notify(snapshot.QuestId, snapshot.PlayerId, progress);
    }

    public static long BeginSubmission(int questId, int itemId, int count)
    {
        long id = ++_nextRequest;
        _pendingSubmissions.Add(id, (questId, itemId, count));
        return id;
    }
    public static H_QuestSubmitT ProcessSubmission(int player, int questId, int itemId, int amount, long requestId)
    {
        if (!RoomManager.IsHost || requestId <= 0) return null;
        if (_submissionResults.TryGetValue((player, requestId), out var previous)) return previous;
        var result = new H_QuestSubmitT { PlayerId = player, QuestId = questId, ItemId = itemId, RequestId = requestId,
            Amount = AddSubmitted(questId, itemId, amount, player) };
        _submissionResults.Add((player, requestId), result);
        return result;
    }
    public static void ApplySubmissionResult(H_QuestSubmitT result)
    {
        if (result.PlayerId != LocalPlayerId || !_pendingSubmissions.TryGetValue(result.RequestId, out var pending)
            || pending.quest != result.QuestId || pending.item != result.ItemId) return;
        _pendingSubmissions.Remove(result.RequestId);
        int refund = pending.count - Math.Max(0, Math.Min(result.Amount, pending.count));
        if (refund > 0) _pendingItems.Add((pending.item, refund));
    }
    public static void ReceiveReward(int questId)
    {
        var quest = QuestTable.Instance.Get(questId);
        if (quest == null || !_receivedRewards.Add(questId)) return;
        foreach (var reward in quest.RewardItems) _pendingItems.Add((reward.itemId, reward.count));
    }
    // 씬 전환 중이거나 인벤토리가 가득 차면 지급 대기량을 보존한다.
    public static void FlushLocalItems()
    {
        for (int i = _pendingItems.Count - 1; i >= 0; i--)
        {
            var item = _pendingItems[i];
            int remaining = item.count - QuestDescriptionUI.TryGiveItem(item.item, item.count);
            if (remaining <= 0) _pendingItems.RemoveAt(i);
            else _pendingItems[i] = (item.item, remaining);
        }
    }
    public static void Clear()
    {
        _progress.Clear(); _pendingSubmissions.Clear(); _submissionResults.Clear();
        _receivedRewards.Clear(); _pendingItems.Clear(); _nextRequest = 0;
    }

    // 기존 공유 저장 형식만 유지한다. 개인 진행의 디스크 저장은 이번 범위에서 제외한다.
    public static SaveData Export()
    {
        var data = new SaveData();
        foreach (var pair in _progress)
        {
            if (pair.Key.owner != 0 || !IsShared(pair.Key.quest)) continue;
            data.entries.Add(new Entry { questId = pair.Key.quest, state = (int)pair.Value.State });
            foreach (var item in pair.Value.Submitted)
                data.submissions.Add(new SubmissionEntry { questId = pair.Key.quest, itemId = item.Key, count = item.Value });
        }
        return data;
    }
    public static void Import(SaveData data)
    {
        Clear();
        if (data == null) return;
        if (data.entries != null) foreach (var entry in data.entries)
            if (IsShared(entry.questId)) Ensure(entry.questId, null).State = (QuestState)entry.state;
        if (data.submissions != null) foreach (var item in data.submissions)
            if (IsShared(item.questId)) Ensure(item.questId, null).Submitted[item.itemId] = item.count;
    }
    [Serializable] public class SaveData { public List<Entry> entries = new(); public List<SubmissionEntry> submissions = new(); }
    [Serializable] public class Entry { public int questId; public int state; }
    [Serializable] public class SubmissionEntry { public int questId; public int itemId; public int count; }
}
