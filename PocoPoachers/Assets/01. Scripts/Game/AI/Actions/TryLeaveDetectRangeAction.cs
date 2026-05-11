using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "TryLeaveDetectRange", story: "[TargetDetector] 인지 범위 밖에 있는지", category: "Action", id: "3f7a1b2c4d5e6f7a8b9c0d1e2f3a4b5c")]
public partial class TryLeaveDetectRangeAction : Action
{
    [SerializeReference] public BlackboardVariable<TargetDetector> TargetDetector;

    protected override Status OnStart()
    {
        // 인지 범위 안이면 Failure, 범위 밖이면 Success
        return TargetDetector.Value.IsTargetInDetectRange() ? Status.Failure : Status.Success;
    }

    protected override void OnEnd() { }
}
