using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CheckTarget", story: "Check [TargetDetector] in range and Set [Target]", category: "Action", id: "f7f20ef888904485c142b9419dc728c9")]
public partial class CheckTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<TargetDetector> TargetDetector;
    [SerializeReference] public BlackboardVariable<PlayerController> Target;

    protected override Status OnStart()
    {
        var go = TargetDetector.Value.DetectedTarget;
        if (go == null || go.GetComponent<PlayerController>() == null) return Status.Failure;


        Target.Value = go.GetComponent<PlayerController>();
        return Status.Success;
    }
}

