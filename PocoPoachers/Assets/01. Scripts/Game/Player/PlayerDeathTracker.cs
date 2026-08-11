using System.Collections.Generic;

// 완전 사망(구조 시간이 끝나 호송 빔 → 관전으로 넘어간 상태)한 플레이어를 기록한다.
// 다운(기절)은 HP 0이지만 구조하면 살아나므로 IsDead만으로는 둘을 구분할 수 없다.
// 완전 사망 시점은 호송 빔 신호(RoomSync.RescueBeamPlay)와 같아, 호스트는 그 신호로 원격 플레이어를 판별한다.
public static class PlayerDeathTracker
{
    static readonly HashSet<int> _finalized = new();

    public static void MarkFinalized(int playerId)
    {
        if (playerId != 0) _finalized.Add(playerId);
    }

    public static void Clear(int playerId) => _finalized.Remove(playerId);

    public static void ClearAll() => _finalized.Clear();

    // 부활하면(HP > 0) 기록이 남아 있어도 살아있는 것으로 본다 — 되살아난 팀원을 계속 빼두면 안 된다.
    public static bool IsFinalized(int playerId) => _finalized.Contains(playerId);
}
