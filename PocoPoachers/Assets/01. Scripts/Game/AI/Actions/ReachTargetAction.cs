using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "ReachTarget", story: "[Self] 이 [Target] 에게 [AttackRange] 거리까지 접근", category: "Action", id: "01c391bc74b7478bdef57bba2050a44e")]
public partial class ReachTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> AttackRange;
    public float RotationSpeed = 10f;
    public float EyeHeight = 1.5f;
    public LayerMask WallLayer;

    private NavMeshAgent _agent;

    protected override Status OnStart()
    {
        if (_agent == null)
            _agent = Self.Value.GetComponent<NavMeshAgent>();

        _agent.updateRotation = false;
        WallLayer = 1 << LayerMask.NameToLayer("Wall");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target.Value == null)
        {
            _agent.ResetPath();
            return Status.Failure;
        }

        float dist = Vector3.Distance(Self.Value.transform.position, Target.Value.transform.position);

        RotateToward(Target.Value.transform.position);

        if (dist <= AttackRange.Value && !IsWallBlocking())
        {
            if (_agent.hasPath)
                _agent.ResetPath();
            return Status.Success;
        }

        _agent.SetDestination(Target.Value.transform.position);
        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (_agent == null) return;
        if (_agent.hasPath)
            _agent.ResetPath();
        _agent.updateRotation = true;
    }

    private bool IsWallBlocking()
    {
        Vector3 eyeOffset = Vector3.up * EyeHeight;
        Vector3 origin = Self.Value.transform.position + eyeOffset;
        Vector3 targetPos = Target.Value.transform.position + eyeOffset;
        Vector3 dir = targetPos - origin;
        return Physics.Raycast(origin, dir.normalized, dir.magnitude, WallLayer);
    }

    private void RotateToward(Vector3 targetPos)
    {
        Vector3 dir = targetPos - Self.Value.transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion target = Quaternion.LookRotation(dir);
        Self.Value.transform.rotation = Quaternion.Slerp(
            Self.Value.transform.rotation, target, RotationSpeed * Time.deltaTime);
    }
}

