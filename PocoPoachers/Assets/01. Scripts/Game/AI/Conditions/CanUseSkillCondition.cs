using System;
using Unity.Behavior;
using UnityEngine;

// 스킬 사용 가능 여부(쿨다운 + 자체 조건)를 BT에서 확인하는 브리지 노드
[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "CanUseSkill", story: "[Self] 가 [Skill] 스킬을 쓸 수 있는지 확인", category: "Conditions", id: "c2b3d4e5f6071829304a5b6c7d8e9f01")]
public partial class CanUseSkillCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<SkillId> Skill;

    public override bool IsTrue()
    {
        if (Self?.Value == null)
            return false;

        var manager = Self.Value.GetComponent<SkillManager>();
        return manager != null && manager.CanUse(Skill.Value);
    }
}
