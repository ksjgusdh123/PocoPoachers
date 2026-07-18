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
    private Transform _appliedTarget;

    protected override Status OnStart()
    {
        if (_rotator == null)
            _rotator = Self.Value.GetComponent<AIRotator>();

        if (_rotator == null || Target?.Value == null)
            return Status.Failure;

        ApplyTarget(Target.Value.transform);
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target?.Value == null)
            return Status.Failure;

        // 노드가 Running으로 계속 도는 동안 블랙보드 타깃이 바뀌면(소리로 더 가까운 대상 교체 등)
        // OnStart는 다시 안 불리므로 여기서 회전 대상을 갱신해야 새 타깃을 바라본다
        Transform current = Target.Value.transform;
        if (current != _appliedTarget)
            ApplyTarget(current);

        return Status.Running;
    }

    private void ApplyTarget(Transform target)
    {
        _appliedTarget = target;
        _rotator.SetTarget(target);
    }

    protected override void OnEnd()
    {
        _rotator?.ClearTarget();
    }
}
