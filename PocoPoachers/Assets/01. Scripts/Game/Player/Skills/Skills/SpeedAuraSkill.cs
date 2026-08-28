using UnityEngine;

// duration 동안 시전자 주변 Data.radius 안에 있는 아군(본인 포함)의 이동속도를 (1 + Data.power)배로 올린다.
// Attack/DefenseAuraSkill과 완전히 같은 구조 — 시전자는 켜짐/꺼짐만 전체에 중계하고(RoomSync.PartyBuff),
// "누가 범위 안에 있는지" 판정과 배율 적용은 각 플레이어의 PartyBuffReceiver가 로컬에서 한다.
// 이동속도는 각 클라가 자기 자신만 시뮬레이션하므로(호스트 판정 불필요) StatSync 전파 대상이 아니다.
public class SpeedAuraSkill : PlayerSkillBase
{
    public override PlayerSkillId Id => PlayerSkillId.SpeedAura;

    private float _elapsed;

    public SpeedAuraSkill(PlayerSkillData data) : base(data) { }

    public override bool CanUse(PlayerSkillContext ctx) => base.CanUse(ctx) && ctx.Stat != null;

    public override void Begin(PlayerSkillContext ctx)
    {
        _elapsed = 0f;
        PartyBuffRegistry.SetActive(RoomSync.MyPlayerId, Data.id, true);
        AuraMeshEffect.SetActiveFor(ctx.Self, PartyBuffRegistry.MaterialResourcePath(Data), true);
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
        AuraMeshEffect.SetActiveFor(ctx.Self, PartyBuffRegistry.MaterialResourcePath(Data), false);
        RoomSync.PartyBuff(Data.id, false);
    }
}
