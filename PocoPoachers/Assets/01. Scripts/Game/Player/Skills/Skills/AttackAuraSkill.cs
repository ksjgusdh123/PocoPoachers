using UnityEngine;

// duration 동안 시전자 주변 Data.radius 안에 있는 아군(본인 포함)의 공격력을 (1 + Data.power)배로 올린다.
// 시전자는 켜짐/꺼짐만 전체에 중계(RoomSync.PartyBuff)하고, "누가 범위 안에 있는지" 판정과 배율 적용은
// 각 플레이어의 PartyBuffReceiver가 로컬에서 한다 — ReflectSkill/InvincibleSkill과 달리 이 스킬 자체는
// 시전자 본인의 스탯을 직접 건드리지 않는다(본인도 같은 판정 경로로 자기 오라 안에 들어와 버프를 받는다).
public class AttackAuraSkill : PlayerSkillBase
{
    public override PlayerSkillId Id => PlayerSkillId.AttackAura;

    private float _elapsed;

    public AttackAuraSkill(PlayerSkillData data) : base(data) { }

    public override bool CanUse(PlayerSkillContext ctx) => base.CanUse(ctx) && ctx.Stat != null;

    public override void Begin(PlayerSkillContext ctx)
    {
        _elapsed = 0f;
        PartyBuffRegistry.SetActive(RoomSync.MyPlayerId, Data.id, true);
        RoomSync.PartyBuff(Data.id, true);
    }

    public override bool Tick(PlayerSkillContext ctx)
    {
        _elapsed += Time.deltaTime;
        return _elapsed < Data.duration;
    }

    public override void End(PlayerSkillContext ctx)
    {
        PartyBuffRegistry.SetActive(RoomSync.MyPlayerId, Data.id, false);
        RoomSync.PartyBuff(Data.id, false);
    }
}
