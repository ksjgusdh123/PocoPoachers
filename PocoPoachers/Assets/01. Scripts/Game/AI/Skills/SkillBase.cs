// 모든 스킬의 공통 뼈대 — SkillData 보유, 쿨다운/기본 CanUse 제공.
// 각 스킬은 Id와 Begin/Tick만 구현하면 되고, 정리할 게 있으면 End를 오버라이드한다.
public abstract class SkillBase : ISkill
{
    protected readonly SkillData Data;

    protected SkillBase(SkillData data)
    {
        Data = data;
    }

    public abstract SkillId Id { get; }
    public float Cooldown => Data.cooldown;

    // 기본: NavMeshAgent만 있으면 사용 가능. 추가 조건이 있으면 오버라이드
    public virtual bool CanUse(SkillContext ctx) => ctx.Agent != null;

    public abstract void Begin(SkillContext ctx);
    public abstract bool Tick(SkillContext ctx);
    public virtual void End(SkillContext ctx) { }
}
