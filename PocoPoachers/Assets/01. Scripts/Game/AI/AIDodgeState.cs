using Unity.Behavior;
using UnityEngine;

// 피격 직전에 "구르고 싶다" 신호를 만들어 BT에 제공. 실제 구르기 처리는 BT의 UseSkill(Dodge) → DodgeRollSkill이 담당.
// 쿨다운/사용 가능 여부는 SkillManager에 위임한다.
[RequireComponent(typeof(EnemyStat))]
[RequireComponent(typeof(SkillManager))]
public class AIDodgeState : MonoBehaviour
{
    // 블랙보드의 bool 변수 이름 — 그래프에서 동일한 이름으로 노출해야 abort 조건이 동작
    [SerializeField] private string _blackboardWantsToDodge = "WantsToDodge";

    public bool WantsToDodge { get; private set; }
    public GameObject LastAttacker { get; private set; }

    private BehaviorGraphAgent _behaviorAgent;
    private SkillManager _skillManager;

    private void Awake()
    {
        _behaviorAgent = GetComponent<BehaviorGraphAgent>();
        _skillManager = GetComponent<SkillManager>();
    }

    // 피격이 들어오는 순간 EnemyStat.TakeDamage가 호출 — 회피 가능하면 구르기를 발동하고 true(데미지 무효)를 반환
    public bool TryEvade(GameObject attacker)
    {
        // 이미 구르기 예약/진행 중이면 데미지만 무효화 (공격자 갱신)
        if (WantsToDodge)
        {
            SetAttacker(attacker);
            return true;
        }

        // 쿨다운/사용 가능 여부는 SkillManager에 위임
        if (_skillManager == null || !_skillManager.CanUse(SkillId.Dodge))
            return false;

        WantsToDodge = true;
        SetAttacker(attacker);
        SyncBlackboard();
        return true;
    }

    // 구르기 스킬 시작 시 호출 — 신호 소비 (쿨다운은 SkillManager가 관리)
    public void ConsumeDodge()
    {
        WantsToDodge = false;
        SyncBlackboard();
    }

    private void SetAttacker(GameObject attacker)
    {
        LastAttacker = attacker;
        _skillManager?.SetAttacker(attacker);
    }

    // 블랙보드 변수와 동기화 — 값이 바뀌는 순간 BT의 abort 조건이 분기를 재평가하게 함
    private void SyncBlackboard()
    {
        if (_behaviorAgent == null) return;
        _behaviorAgent.BlackboardReference.SetVariableValue(_blackboardWantsToDodge, WantsToDodge);
    }
}
