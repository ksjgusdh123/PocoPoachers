using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "IsTargetInAttackRange", story: "[Self]의 [AttackRange] 안에 [Target] 이 있는지 확인", category: "Conditions", id: "a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6")]
public partial class IsTargetInAttackRangeCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> AttackRange;

    public override bool IsTrue()
    {
        if (Self?.Value == null) return false;
        if (Target?.Value == null) return false;

        float distanceSqr = (Target.Value.transform.position - Self.Value.transform.position).sqrMagnitude;
        return distanceSqr <= AttackRange.Value * AttackRange.Value;
    }
}
