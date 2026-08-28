using System;
using UnityEngine;

// 팀원 버프 오라 수신 판정 — 시전자 스킬(AttackAuraSkill 등)은 켜짐/꺼짐만 전체에 알리고(PartyBuffRegistry),
// "누가 지금 범위 안에 있는지"는 이 클라이언트가 매 틱 스스로 계산한다. 플레이어 위치는 이미 서로
// 동기화돼 보이므로 좌표를 따로 실을 필요가 없고, skill_id로 반경·배율을 player_skill.csv에서 직접 읽는다.
// PlayerSkillManager(로컬 플레이어에만 존재)가 소유하고 매 프레임 Tick()을 불러준다 — 그래서 이 클래스의
// 인스턴스는 클라이언트당 정확히 1개다.
//
// 두 가지 일을 한다:
// 1) 내 스탯 배율 적용 — 나 자신만 할 수 있다(내 StatBase를 직접 건드리고 StatSync로 전파해야 하므로).
// 2) 오라 비주얼 갱신 — 나 + 화면에 보이는 다른 모든 플레이어에 대해서도 한다. 위치는 이미 서로에게
//    보이는 정보라 별도 네트워크 통신 없이 각자 클라이언트에서 독립적으로 같은 결론을 낼 수 있다.
//    그래서 시전자 본인(항상 자기 오라 반경 0 안)은 물론, 범위에 들어오고 나가는 팀원도 자연히
//    켜지고 꺼진다 — 스킬 Begin/End나 G·H_PartyBuff 핸들러가 비주얼을 직접 건드릴 필요가 없다.
public class PartyBuffReceiver
{
    private const float CheckInterval = 0.25f;

    private static readonly string AttackAuraMaterialPath = $"Skill/{nameof(PlayerSkillId.AttackAura)}Material";
    private static readonly string DefenseAuraMaterialPath = $"Skill/{nameof(PlayerSkillId.DefenseAura)}Material";
    private static readonly string SpeedAuraMaterialPath = $"Skill/{nameof(PlayerSkillId.SpeedAura)}Material";

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

        var self = EvaluateAt(myPos);
        ApplySelfStats(self);
        ApplyVisual(_transform.gameObject, self);

        if (ObjectManager.Instance == null) return;

        foreach (var obj in ObjectManager.Instance.GetAllByKind(ObjectKind.Player))
        {
            if (obj == null || obj.Id == myId) continue;

            var result = EvaluateAt(obj.transform.position);
            ApplyVisual(obj.gameObject, result);
        }
    }

    private void ApplySelfStats((float attack, float defense, float speed) result)
    {
        bool changed = false;
        if (!Mathf.Approximately(result.attack, _stat.AttackPowerMultiplier))
        {
            _stat.AttackPowerMultiplier = result.attack;
            changed = true;
        }
        if (!Mathf.Approximately(result.defense, _stat.DefenseBuffRate))
        {
            _stat.DefenseBuffRate = result.defense;
            changed = true;
        }
        if (!Mathf.Approximately(result.speed, _stat.MoveSpeedBuffMultiplier))
        {
            // 이동속도는 네트워크 판정 대상이 아니라 SyncStatsNow가 필요 없지만, 나머지와 같이 한 번에
            // 처리해도 무해하다(StatSync는 솔로에선 아예 아무것도 보내지 않는다).
            _stat.MoveSpeedBuffMultiplier = result.speed;
            changed = true;
        }

        if (changed) _stat.SyncStatsNow();
    }

    private static void ApplyVisual(GameObject target, (float attack, float defense, float speed) result)
    {
        AuraMeshEffect.SetActiveFor(target, AttackAuraMaterialPath, result.attack > StatBase.DefaultAttackPowerMultiplier);
        AuraMeshEffect.SetActiveFor(target, DefenseAuraMaterialPath, result.defense > StatBase.DefaultDefenseBuffRate);
        AuraMeshEffect.SetActiveFor(target, SpeedAuraMaterialPath, result.speed > PlayerStat.DefaultMoveSpeedBuffMultiplier);
    }

    // 주어진 위치가 현재 켜져 있는 팀원 버프 오라들의 범위 안에 있는지 판정해 배율/부가 방어율을 계산한다.
    // 새 버프가 추가되면 여기 분기와 switch만 늘리면 된다.
    private (float attack, float defense, float speed) EvaluateAt(Vector3 pos)
    {
        float attackMultiplier = StatBase.DefaultAttackPowerMultiplier;
        float defenseBuffRate = StatBase.DefaultDefenseBuffRate;
        float speedMultiplier = PlayerStat.DefaultMoveSpeedBuffMultiplier;

        foreach (var (playerId, skillId) in PartyBuffRegistry.ActiveSources)
        {
            PlayerSkillData data = PlayerSkillTable.Instance.Get(skillId);
            if (data == null) continue;
            if (!Enum.TryParse(data.skill, true, out PlayerSkillId id)) continue;
            if (id != PlayerSkillId.AttackAura && id != PlayerSkillId.DefenseAura && id != PlayerSkillId.SpeedAura)
                continue;

            if (!TryGetPlayerPosition(playerId, out Vector3 sourcePos)) continue;
            if (Vector3.Distance(pos, sourcePos) > data.radius) continue;

            switch (id)
            {
                case PlayerSkillId.AttackAura:
                    attackMultiplier = Mathf.Max(attackMultiplier, 1f + data.power);
                    break;
                case PlayerSkillId.DefenseAura:
                    defenseBuffRate = Mathf.Max(defenseBuffRate, data.power);
                    break;
                case PlayerSkillId.SpeedAura:
                    speedMultiplier = Mathf.Max(speedMultiplier, 1f + data.power);
                    break;
            }
        }

        return (attackMultiplier, defenseBuffRate, speedMultiplier);
    }

    private bool TryGetPlayerPosition(int playerId, out Vector3 pos)
    {
        if (playerId == RoomSync.MyPlayerId)
        {
            pos = _transform.position;
            return true;
        }

        if (ObjectManager.Instance != null && ObjectManager.Instance.TryGet(ObjectKind.Player, playerId, out var obj))
        {
            pos = obj.transform.position;
            return true;
        }

        pos = default;
        return false;
    }
}
