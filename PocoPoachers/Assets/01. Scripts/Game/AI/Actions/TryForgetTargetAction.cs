using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "TryForgetTarget", story: "[TargetDetector의] 잊는 범위 밖에 있는지", category: "Action", id: "dc24484a197a1cc0e5d5fae8c893780b")]
public partial class TryForgetTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<TargetDetector> TargetDetector의;

    protected override Status OnStart()
    {
        // 범위 밖이면(잊음) Success, 아직 범위 안이면 Failure
        return TargetDetector의.Value.IsTargetInForgetRange() ? Status.Failure : Status.Success;
    }

    protected override void OnEnd()
    {
    }
}

