using UnityEngine;
using UnityEngine.AI;

// 스킬 실행에 필요한 참조 묶음 — SkillManager가 1회 생성해 모든 스킬에 전달.
public class SkillContext
{
    public GameObject Self { get; }
    public NavMeshAgent Agent { get; }
    public EnemyStat Stat { get; }
    public AIRotator Rotator { get; }
    public Animator Animator { get; }

    // 현재 전투 타겟 — TargetDetector에서 실시간 조회
    public GameObject Target => _detector != null ? _detector.CurrentTarget : null;
    // 직전 공격자 — 피격 시 AIDodgeState가 채움
    public GameObject Attacker { get; set; }

    private readonly TargetDetector _detector;

    public SkillContext(GameObject self)
    {
        Self = self;
        Agent = self.GetComponent<NavMeshAgent>();
        Stat = self.GetComponent<EnemyStat>();
        Rotator = self.GetComponent<AIRotator>();
        Animator = self.GetComponentInChildren<Animator>();
        _detector = self.GetComponent<TargetDetector>();
    }
}
