using UnityEngine;

// duration 대기 후 power 만큼 회복. 대기 중 사망하면 회복 없이 종료.
public class HealSkill : SkillBase
{
    public override SkillId Id => SkillId.Heal;

    private float _elapsed;

    public HealSkill(SkillData data) : base(data) { }

    public override bool CanUse(SkillContext ctx) => ctx.Stat != null && !ctx.Stat.IsDead;

    public override void Begin(SkillContext ctx)
    {
        _elapsed = 0f;
    }

    public override bool Tick(SkillContext ctx)
    {
        if (ctx.Stat == null || ctx.Stat.IsDead)
            return false;

        _elapsed += Time.deltaTime;
        if (_elapsed < Data.duration)
            return true;

        ctx.Stat.Heal(Data.power);
        return false;
    }
}
