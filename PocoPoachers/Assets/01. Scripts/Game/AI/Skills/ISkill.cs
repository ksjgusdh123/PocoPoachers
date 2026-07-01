// AI 스킬. 쿨다운은 SkillManager가 관리하므로 CanUse는 그 외 조건만 판단한다.
public interface ISkill
{
    SkillId Id { get; }
    float Cooldown { get; }

    bool CanUse(SkillContext ctx);
    void Begin(SkillContext ctx);
    bool Tick(SkillContext ctx); // true=진행중, false=종료
    void End(SkillContext ctx);
}
