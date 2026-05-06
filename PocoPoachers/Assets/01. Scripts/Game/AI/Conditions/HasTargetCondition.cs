using System;
using Unity.Behavior;
using UnityEngine;

[Serializable, Unity.Properties.GeneratePropertyBag]
[Condition(name: "HasTarget", story: "Agent Detect Correct [Target]", category: "Conditions", id: "83be96d2fc615ad3b5867ff1e3f30f49")]
public partial class HasTargetCondition : Condition
{
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    public override bool IsTrue()
    {
        return Target.Value == null ? true : false;
    }
}
