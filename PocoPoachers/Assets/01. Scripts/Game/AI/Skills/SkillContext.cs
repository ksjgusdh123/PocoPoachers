using Unity.Behavior;
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
    private readonly BehaviorGraphAgent _behaviorAgent;

    public SkillContext(GameObject self)
    {
        Self = self;
        Agent = self.GetComponent<NavMeshAgent>();
        Stat = self.GetComponent<EnemyStat>();
        Rotator = self.GetComponent<AIRotator>();
        Animator = self.GetComponentInChildren<Animator>();
        _detector = self.GetComponent<TargetDetector>();
        _behaviorAgent = self.GetComponent<BehaviorGraphAgent>();
    }

    // 블랙보드 변수 세팅 (해당 이름의 변수가 그래프에 있어야 함)
    public void SetBlackboard<T>(string name, T value)
    {
        _behaviorAgent?.BlackboardReference.SetVariableValue(name, value);
    }

    // 블랙보드 변수 읽기 — 변수가 없으면 false
    public bool TryGetBlackboard<T>(string name, out T value)
    {
        if (_behaviorAgent != null)
            return _behaviorAgent.BlackboardReference.GetVariableValue(name, out value);
        value = default;
        return false;
    }
}
