using System;
using UnityEngine;

// 팀원 버프 오라 수신 판정 — 시전자 스킬(AttackAuraSkill 등)은 켜짐/꺼짐만 전체에 알리고(PartyBuffRegistry),
// "내가 지금 범위 안에 있는지"는 버프를 받는 이 플레이어 자신이 매 틱 스스로 판정한다.
// 플레이어 위치는 이미 서로 동기화돼 보이므로 좌표를 따로 실을 필요가 없고, skill_id로 반경·배율을
// player_skill.csv에서 직접 읽는다. PlayerSkillManager가 소유하고 매 프레임 Tick()을 불러준다.
public class PartyBuffReceiver
{
    private const float CheckInterval = 0.25f;

    private readonly Transform _transform;
    private readonly PlayerStat _stat;
    private float _timer;

    public PartyBuffReceiver(Transform transform, PlayerStat stat)
    {
        _transform = transform;
        _stat = stat;
    }

    public void Tick()
    {
        _timer -= Time.deltaTime;
        if (_timer > 0f) return;
        _timer = CheckInterval;

        Evaluate();
    }

    private void Evaluate()
    {
        if (_stat == null) return;

        int myId = RoomSync.MyPlayerId;
        Vector3 myPos = _transform.position;
        float attackMultiplier = StatBase.DefaultAttackPowerMultiplier;
        float defenseBuffRate = StatBase.DefaultDefenseBuffRate;

        foreach (var (playerId, skillId) in PartyBuffRegistry.ActiveSources)
        {
            PlayerSkillData data = PlayerSkillTable.Instance.Get(skillId);
            if (data == null) continue;
            if (!Enum.TryParse(data.skill, true, out PlayerSkillId id)) continue;
            if (id != PlayerSkillId.AttackAura && id != PlayerSkillId.DefenseAura)
                continue; // 이동속도 오라 등이 추가되면 여기 분기와 아래 switch만 늘리면 된다

            if (!TryGetSourcePosition(playerId, myId, myPos, out Vector3 sourcePos)) continue;
            if (Vector3.Distance(myPos, sourcePos) > data.radius) continue;

            switch (id)
            {
                case PlayerSkillId.AttackAura:
                    attackMultiplier = Mathf.Max(attackMultiplier, 1f + data.power);
                    break;
                case PlayerSkillId.DefenseAura:
                    defenseBuffRate = Mathf.Max(defenseBuffRate, data.power);
                    break;
            }
        }

        bool changed = false;
        if (!Mathf.Approximately(attackMultiplier, _stat.AttackPowerMultiplier))
        {
            _stat.AttackPowerMultiplier = attackMultiplier;
            changed = true;
        }
        if (!Mathf.Approximately(defenseBuffRate, _stat.DefenseBuffRate))
        {
            _stat.DefenseBuffRate = defenseBuffRate;
            changed = true;
        }

        if (changed) _stat.SyncStatsNow();
    }

    private static bool TryGetSourcePosition(int playerId, int myId, Vector3 myPos, out Vector3 sourcePos)
    {
        if (playerId == myId)
        {
            sourcePos = myPos;
            return true;
        }

        if (ObjectManager.Instance != null && ObjectManager.Instance.TryGet(ObjectKind.Player, playerId, out var obj))
        {
            sourcePos = obj.transform.position;
            return true;
        }

        sourcePos = default;
        return false;
    }
}
