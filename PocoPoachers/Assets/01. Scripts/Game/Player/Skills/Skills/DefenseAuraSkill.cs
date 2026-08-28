using UnityEngine;

// duration 동안 시전자 주변 Data.radius 안에 있는 아군(본인 포함)에게 Data.power만큼의 부가 방어율을 준다.
// AttackAuraSkill과 완전히 같은 구조 — 시전자는 켜짐/꺼짐만 전체에 중계하고(RoomSync.PartyBuff),
// "누가 범위 안에 있는지" 판정·배율 적용·오라 비주얼까지 전부 PartyBuffReceiver가 로컬에서 한다.
public class DefenseAuraSkill : PlayerSkillBase
{
    public override PlayerSkillId Id => PlayerSkillId.DefenseAura;

    private float _elapsed;

    public DefenseAuraSkill(PlayerSkillData data) : base(data) { }

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
