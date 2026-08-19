public abstract class PlayerSkillBase : IPlayerSkill
{
    public PlayerSkillData Data { get; }

    protected PlayerSkillBase(PlayerSkillData data)
    {
        Data = data;
    }

    public abstract PlayerSkillId Id { get; }
    public float Cooldown => Data.cooldown;
    public virtual bool LocksMovement => false;

    public virtual bool CanUse(PlayerSkillContext ctx)
    {
        if (ctx.Stat == null || ctx.Stat.IsDead) return false;
        return ctx.Dodge == null || !ctx.Dodge.IsRolling;
    }

    public abstract void Begin(PlayerSkillContext ctx);
    public abstract bool Tick(PlayerSkillContext ctx);
    public virtual void End(PlayerSkillContext ctx) { }
}
