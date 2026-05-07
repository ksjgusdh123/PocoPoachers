using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "PatrolRandomPosition", story: "[Self] patrols random position within [Radius]", category: "Action", id: "b49a3dbcf6bf155baa951a347c238d6d")]
public partial class PatrolRandomPositionAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<float> Radius;
    [SerializeReference] public BlackboardVariable<PlayerController> Target;

    private NavMeshAgent _agent;

    protected override Status OnStart()
    {
        if (_agent == null)
            _agent = Self.Value.GetComponent<NavMeshAgent>();

        _agent.SetDestination(GetRandomNavMeshPosition());
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target.Value != null)
        {
            _agent.ResetPath();
            return Status.Failure;
        }

        if (_agent.pathPending) return Status.Running;

        if (_agent.remainingDistance <= _agent.stoppingDistance)
            return Status.Success;

        return Status.Running;
    }

    protected override void OnEnd()
    {
    }

    private Vector3 GetRandomNavMeshPosition()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDir = _agent.transform.position + UnityEngine.Random.insideUnitSphere * Radius.Value;
            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, Radius.Value, NavMesh.AllAreas))
                return hit.position;
        }
        return _agent.transform.position;
    }
}
