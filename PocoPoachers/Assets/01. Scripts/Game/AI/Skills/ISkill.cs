// AI 스킬 인터페이스 — "어떻게" 동작하는지를 구현. "언제" 쓸지는 BT가 결정
// 쿨다운은 SkillManager가 일원 관리하므로, CanUse는 쿨다운 외 상황/자원 조건만 판단
public interface ISkill
{
    SkillId Id { get; }
    float Cooldown { get; }

    // 쿨다운 외 발동 가능 조건 (자원, 상태, 대상 유무 등)
    bool CanUse(SkillContext ctx);

    // 발동 시작
    void Begin(SkillContext ctx);

    // 매 프레임 진행 — true면 계속 진행 중, false면 종료
    bool Tick(SkillContext ctx);

    // 종료 정리 (정상 종료 / 중단 공통)
    void End(SkillContext ctx);
}
