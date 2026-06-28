using System;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.AI;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "RetreatFromTarget", story: "[Self] 가 [Target] 으로부터 [RetreatDistance] 만큼 후퇴", category: "Action", id: "9d4e1a2b3c4d5e6f7a8b9c0d1e2f3a4b")]
public partial class RetreatFromTargetAction : Action
{
    [SerializeReference] public BlackboardVariable<GameObject> Self;
    [SerializeReference] public BlackboardVariable<GameObject> Target;
    [SerializeReference] public BlackboardVariable<float> RetreatDistance;

    private NavMeshAgent _agent;
    private float _repathTimer;
    private const float RepathInterval = 0.5f;

    protected override Status OnStart()
    {
        if (_agent == null)
            _agent = Self.Value.GetComponent<NavMeshAgent>();

        // 이전 액션(예: RotateToTarget)이 AIRotator로 회전을 가져간 상태일 수 있으므로
        // 후퇴 중에는 에이전트가 이동(후퇴) 방향을 직접 보도록 되돌림
        _agent.updateRotation = true;

        Self.Value.GetComponent<EnemyStat>()?.MarkRetreated();

        _repathTimer = 0f;
        SetRetreatDestination();
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        if (Target.Value == null)
        {
            _agent.ResetPath();
            return Status.Success;
        }

        float distance = Vector3.Distance(Self.Value.transform.position, Target.Value.transform.position);
        if (distance >= RetreatDistance.Value)
        {
            _agent.ResetPath();
            return Status.Success;
        }

        _repathTimer -= Time.deltaTime;
        if (_repathTimer <= 0f)
            SetRetreatDestination();

        return Status.Running;
    }

    protected override void OnEnd()
    {
        if (_agent == null) return;
        _agent.updateRotation = false;
        if (_agent.hasPath)
            _agent.ResetPath();
    }

    private void SetRetreatDestination()
    {
        _repathTimer = RepathInterval;

        Vector3 selfPos = Self.Value.transform.position;
        Vector3 awayDir = (selfPos - Target.Value.transform.position).normalized;
        Vector3 desiredPos = selfPos + awayDir * RetreatDistance.Value;

        if (NavMesh.SamplePosition(desiredPos, out NavMeshHit hit, RetreatDistance.Value, NavMesh.AllAreas))
            _agent.SetDestination(hit.position);
    }
}
