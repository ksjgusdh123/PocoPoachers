// 스킬 공통 뼈대 — SkillData 보유, 쿨다운/기본 CanUse 제공.
public abstract class SkillBase : ISkill
{
    protected readonly SkillData Data;

    protected SkillBase(SkillData data)
    {
        Data = data;
    }

    public abstract SkillId Id { get; }
    public float Cooldown => Data.cooldown;

    public virtual bool CanUse(SkillContext ctx) => ctx.Agent != null;
    public abstract void Begin(SkillContext ctx);
    public abstract bool Tick(SkillContext ctx);
    public virtual void End(SkillContext ctx) { }
}
