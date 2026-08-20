// 플레이어 스킬. 쿨다운은 PlayerSkillManager가 관리하므로 CanUse는 그 외 조건만 판단한다.
public interface IPlayerSkill
{
    PlayerSkillId Id { get; }
    PlayerSkillData Data { get; }
    float Cooldown { get; }

    // 진행 중 PlayerMovement의 수평 이동을 막고 스킬이 직접 이동시킨다 (대시 등)
    bool LocksMovement { get; }

    bool CanUse(PlayerSkillContext ctx);
    void Begin(PlayerSkillContext ctx);
    bool Tick(PlayerSkillContext ctx); // true=진행중, false=종료
    void End(PlayerSkillContext ctx);
}
