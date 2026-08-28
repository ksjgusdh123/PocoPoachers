using System.Collections.Generic;

// 현재 켜져 있는 팀원 버프 오라 시전자 목록 — (시전자 playerId, skillId) 쌍으로 켜짐만 기록한다.
// 실제로 "누가 범위 안에 있는지" 판정은 각 플레이어의 PartyBuffReceiver가 매 틱 이 목록을 훑어서 한다.
// 반경·배율 같은 수치는 여기 담지 않고 skillId로 player_skill.csv에서 그때그때 읽는다 —
// 시전자가 로컬에서 즉시 등록하고, 다른 클라는 G_PartyBuff/H_PartyBuff 통보로 등록/해제한다.
public static class PartyBuffRegistry
{
    private static readonly HashSet<(int playerId, int skillId)> _active = new();

    public static void SetActive(int playerId, int skillId, bool active)
    {
        var key = (playerId, skillId);
        if (active) _active.Add(key);
        else _active.Remove(key);
    }

    public static IEnumerable<(int playerId, int skillId)> ActiveSources => _active;
}
