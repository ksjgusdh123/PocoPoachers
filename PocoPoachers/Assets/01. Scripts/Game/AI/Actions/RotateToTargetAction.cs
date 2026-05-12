using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RotateToTarget", story: "[Self] 이 [Target] 방향으로 회전", category: "Action", id: "a1b2c3d4e5f6471a8b9c0d1e2f3a4b5c")]
public partial class RotateToTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;

    private AIRotator _rotator;

    protected override Status OnStart()
    {
        if (_rotator == null)
            _rotator = Self.Value.GetComponent<AIRotator>();

        if (_rotator == null || Target?.Value == null)
            return Status.Failure;

        _rotator.SetTarget(Target.Value.transform);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target?.Value == null)
            return Status.Failure;

        return Status.Running;
    }

    protected override void OnEnd()
    {
        _rotator?.ClearTarget();
    }
}
