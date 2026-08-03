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
    private EnemyPatrolBounds _bounds;
    private Vector3 _spawnPosition;
    private bool _hasSpawnPosition;

    protected override Status OnStart()
    {
        if (_agent == null)
            _agent = Self.Value.GetComponent<NavMeshAgent>();
        if (_bounds == null)
            _bounds = Self.Value.GetComponent<EnemyPatrolBounds>();

        // EnemySpawner가 지정한 스폰 기준점이 있으면 그것을, 없으면 첫 실행 시점의 위치를 기준점으로 사용
        if (!_hasSpawnPosition)
        {
            _spawnPosition = _bounds != null && _bounds.IsSet ? _bounds.Origin : _agent.transform.position;
            _hasSpawnPosition = true;
        }

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
        float radius = _bounds != null && _bounds.IsSet ? _bounds.Radius : Radius.Value;

        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDir = _spawnPosition + UnityEngine.Random.insideUnitSphere * radius;
            if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, radius, NavMesh.AllAreas))
                return hit.position;
        }
        return _agent.transform.position;
    }
}
