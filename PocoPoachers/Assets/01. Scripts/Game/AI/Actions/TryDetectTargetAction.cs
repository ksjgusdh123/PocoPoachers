using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "TryDetectTarget", story: "[AI] Try to Detect [Target]", category: "Action", id: "b9c5cac3581ee5bba23a64ba3f6ef97f")]
public partial class TryDetectTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<TargetDetector> AI;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    protected override Status OnStart()
    {
        return AI.Value.TryDetect() ? Status.Success : Status.Failure;
    }
}

