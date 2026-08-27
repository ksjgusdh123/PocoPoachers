using UnityEngine;

// 장착하고 있는 동안 player_skill.csv의 passive_stat에 passive_value만큼 보너스를 얹는 패시브 스킬.
// 발동 개념이 없어 쿨다운도 지속시간도 쓰지 않는다.
// 보너스는 PlayerEnhancement에 등록만 하면 되고, 실제 반영(체력·이속·방어·시야·총기 배율)은
// 강화 스탯과 같은 경로를 그대로 탄다.
public class PassiveStatSkill : PlayerSkillBase
{
    public override PlayerSkillId Id => PlayerSkillId.PassiveStat;
    public override bool IsPassive => true;

    public PassiveStatSkill(PlayerSkillData data) : base(data) { }

    public override void OnEquip(PlayerSkillContext ctx)
    {
        if (ctx.Enhancement == null) return;
        if (!Data.TryGetPassive(out EnhancementStatType statType, out float value)) return;

        ctx.Enhancement.SetPassiveBonus(Data.id, statType, value);
    }

    public override void OnUnequip(PlayerSkillContext ctx)
    {
        ctx.Enhancement?.ClearPassiveBonus(Data.id);
    }

    public override bool CanUse(PlayerSkillContext ctx) => false;

    public override void Begin(PlayerSkillContext ctx) { }

    public override bool Tick(PlayerSkillContext ctx) => false;
}
